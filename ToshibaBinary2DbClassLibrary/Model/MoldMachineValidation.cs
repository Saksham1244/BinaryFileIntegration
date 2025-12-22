using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;

namespace ToshibaBinary2DbClassLibrary.Model
{
    public class MoldMachineValidation
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        MldMacValid mldMacValid;
        MoldMachineValidation_Data MMVD;
        Configuration CFG;


        public MoldMachineValidation(MldMacValid _mldMacValid)
        {
            //byte[] Machine_Id_Bytes = Encoding.ASCII.GetBytes(Machine_Id);
            //byte[] Mold_Id_Bytes = Encoding.ASCII.GetBytes(Mold_Id);

            mldMacValid = new MldMacValid { Machine_Id = _mldMacValid.Machine_Id, Mold_Id = _mldMacValid.Mold_Id, Valid_Status = _mldMacValid.Valid_Status, Reserve = _mldMacValid.Reserve };
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);
        }

        public MoldMachineValidation()
        {
            MMVD = new MoldMachineValidation_Data();
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);
        }
        public string DownloadValidationFile(string Machine_Id)
        {
            try
            {
                var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;               
                
               
                string MachineFolder = Path.Combine(LocalFilePath, Machine_Id);

                //Console.WriteLine("DownloadValidationFile" );
                //Console.WriteLine(MachineFolder);
                
                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                //Console.WriteLine(userName);

                string ValidationFile_Name = MachineFolder + "mold_validation\\" + "MldMacValid" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".vld";
                string ValidationFile_Folder = Path.Combine(MachineFolder, "mold_validation");
                //Console.WriteLine(ValidationFile_Folder);

                if (!Directory.Exists(ValidationFile_Folder))
                {
                    Directory.CreateDirectory(ValidationFile_Folder);
                    //System.Console.WriteLine($"Folder '{ValidationFile_Folder}' created.");
                }

                byte[] ValidationFile_Bytes = BinaryReaderExtensions.WriteByteArray(mldMacValid);
                System.IO.BinaryWriter binWriter = new System.IO.BinaryWriter(System.IO.File.Open(ValidationFile_Name, System.IO.FileMode.Create));
                binWriter.Flush();
                binWriter.Write(ValidationFile_Bytes); // just feed it the contents verbatim
                binWriter.Close();

                return ValidationFile_Name;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                //ApplicationLogs.WriteLog(ex.Message);
                //Console.WriteLine(ex.StackTrace);
                return null;
            }


        }

        public void read_MldMacVld_Files(string Machine_ID, string LocalFilePath)
        {
            try
            {


                var cs = CFG.AppSettings.Settings["ConnectionString"].Value;
                //var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;
                //var MachConfigFilePath = CFG.AppSettings.Settings["MachConfigFilePath"].Value;

                //open the XML File having the Machine configuration
                //XmlDocument doc = new XmlDocument();
                //doc.Load(MachConfigFilePath);

                //foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                //{

                    //string ftpAddress = node.Attributes["Machine_IP"].Value;
                    //string filePathOnFtp = node.Attributes["Machine_Ftp_Path"].Value;
                    //string username = node.Attributes["Machine_Ftp_ID"].Value;
                    //string password = node.Attributes["Machine_Ftp_Pwd"].Value;

                    //string Machine_ID = node.Attributes["Machine_ID"].Value;
                    string MachineFolder = LocalFilePath + "\\" + Machine_ID + "\\";

                    string folderPath = $"{MachineFolder}mold_validation";
                    string readFolderPath = $"{MachineFolder}mold_validation\\read\\";

                using (IDbConnection db = new SqlConnection(cs))
                {
                    foreach (string fileName in Directory.EnumerateFiles(folderPath, "*.vld"))
                    {
                        string InsertMoldMachValid_DataTable = @"INSERT INTO [dbo].[MoldMachineValidationData]
                                                                       (
		                                                               [Machine_Id]
                                                                       ,[Mold_Id]
                                                                       ,[Valid_Status]
                                                                       ,[Reserve]
                                                                       ,[ProdDate]
                                                                       ,[ShiftName]

		                                                               )
                                                                 VALUES
                                                                       (
		                                                               @Machine_Id
                                                                       ,@Mold_Id
                                                                       ,@Valid_Status
                                                                       ,@Reserve
                                                                       ,@ProdDate
                                                                       ,@ShiftName

		                                                               )";

                        string getProdDateShift = @"select ProdDate,ShiftName from Prod_ShiftInformation 
                                                where StationID=(
                                                select StationID from Config_Equipment where EquipmentID=@EquipmentID)";
                        ProdDateAndShift prodDateAndShift = new ProdDateAndShift();

                        try
                        {
                            prodDateAndShift = db.QueryFirst<ProdDateAndShift>(getProdDateShift, new { EquipmentID = Machine_ID });
                        }
                        catch (Exception queryEx)
                        {
                            logger.Warn($"Could not find ProdDate/Shift for Machine {Machine_ID}: {queryEx.Message}");
                            Console.WriteLine($"WARNING: Could not find ProdDate/Shift for Machine {Machine_ID}, using defaults");
                            // Set defaults if query fails
                            prodDateAndShift.ProdDate = DateTime.Now.Date;
                            prodDateAndShift.ShiftName = "A";
                        }

                        read_Validation_File(fileName);
                        //SET Machine Id , Prod Date and Shift name 
                        MMVD.Machine_Id = Machine_ID;
                        MMVD.ProdDate = prodDateAndShift.ProdDate;
                        MMVD.ShiftName = prodDateAndShift.ShiftName;

                        int rowsAffected = db.Execute(InsertMoldMachValid_DataTable, MMVD);
                        if (rowsAffected > 0)
                        {
                            Directory.CreateDirectory(readFolderPath);
                            string readFileName = fileName.Replace($"\\mold_validation", $"\\mold_validation\\read");
                            //moving file
                            File.Move(fileName, readFileName);
                        }
                        //Console.WriteLine(rowsAffected);
                    }
                }
                //}
                //Console.ReadLine();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                //ApplicationLogs.WriteLog(ex.Message);
                
            }
        }


        void read_Validation_File(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {

                    MldMacValid validationData = new MldMacValid();
                    validationData = reader.ReadClass<MldMacValid>();

                    MMVD.Machine_Id = validationData.Machine_Id;
                    MMVD.Mold_Id = validationData.Mold_Id;
                    MMVD.Valid_Status = validationData.Valid_Status.ToString();
                    MMVD.Reserve = validationData.Reserve.ToString();
                }

            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MldMacValid
    {

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string Machine_Id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Mold_Id;

        public int Valid_Status;
        public int Reserve;

    }

    public class MoldMachineValidation_Data
    {
        public decimal NID { get; set; }
        public string Machine_Id { get; set; }
        public string Mold_Id { get; set; }
        public string Valid_Status { get; set; }
        public string Reserve { get; set; }
        public DateTime ProdDate { get; set; }
        public string ShiftName { get; set; }
    }

}
