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
using System.Xml;

namespace ToshibaBinary2DbClassLibrary.Model
{
    public class Machine_Data
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();
        Machine_Detail_Data MDD;
        Configuration CFG;

        public Machine_Data()
        {

            MDD=new Machine_Detail_Data();
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);

        }

        public void read_MAC_Files(string Machine_ID, string LocalFilePath)
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

                    string folderPath = $"{MachineFolder}mac_para";
                    string readFolderPath = $"{MachineFolder}mac_para\\read\\";
                


                    foreach (string fileName in Directory.EnumerateFiles(folderPath, "*.mac"))
                    {

                        string InsertMachine_Mac_DataTable = @"INSERT INTO [dbo].[Machine_Data]
                                                               (
                                                                [Machine_Name]
                                                               ,[Machine_Serial_Number]
                                                               ,[Machine_Configuration]
                                                               ,[Mould_ID]
                                                               ,[Part_Number]
                                                               ,[Material_Name]
                                                               ,[Machine_Mode]
                                                               ,[Cavity_Count]
                                                               ,[Pump_Status]
                                                               ,[Heater_Status]
                                                               ,[Shot_Weight]
                                                               ,[Total_Good_Parts_Produced]
                                                               ,[Total_Rejects]
                                                               ,[Total_Good_Shots]
                                                               ,[Total_Shots]
                                                               ,[Bin_Good_Parts_Produced]
                                                               ,[Bin_Rejects]
                                                               ,[Bin_Good_Shots]
                                                               ,[Bin_Total_Shots]
                                                               ,[Set_Good_Parts]
                                                               ,[Energy_Value]
                                                               ,[Ideal_Cycle_Time]
                                                               ,[Machine_Id]
                                                               ,[ProdDate]
                                                               ,[ShiftName])
                                                         VALUES
                                                               (
	                                                            @Machine_Name
                                                               ,@Machine_Serial_Number
                                                               ,@Machine_Configuration
                                                               ,@Mould_ID
                                                               ,@Part_Number
                                                               ,@Material_Name
                                                               ,@Machine_Mode
                                                               ,@Cavity_Count
                                                               ,@Pump_Status
                                                               ,@Heater_Status
                                                               ,@Shot_Weight
                                                               ,@Total_Good_Parts_Produced
                                                               ,@Total_Rejects
                                                               ,@Total_Good_Shots
                                                               ,@Total_Shots
                                                               ,@Bin_Good_Parts_Produced
                                                               ,@Bin_Rejects
                                                               ,@Bin_Good_Shots
                                                               ,@Bin_Total_Shots
                                                               ,@Set_Good_Parts
                                                               ,@Energy_Value
                                                               ,@Ideal_Cycle_Time
                                                               ,@Machine_Id
                                                               ,@ProdDate
                                                               ,@ShiftName
	                                                    )";
                        string getProdDateShift = @"select ProdDate,ShiftName from Prod_ShiftInformation 
                                                where StationID=(
                                                select StationID from Config_Equipment where EquipmentID=@EquipmentID)";
                        ProdDateAndShift prodDateAndShift = new ProdDateAndShift();


                        using (IDbConnection db = new SqlConnection(cs))
                        {
                            prodDateAndShift = db.QueryFirst<ProdDateAndShift>(getProdDateShift, new { EquipmentID = Machine_ID });
                            read_MAC_File(fileName);
                            //SET Machine Id , Prod Date and Shift name 
                            MDD.Machine_Id = Machine_ID;
                            MDD.ProdDate = prodDateAndShift.ProdDate;
                            MDD.ShiftName = prodDateAndShift.ShiftName;



                            int rowsAffected = db.Execute(InsertMachine_Mac_DataTable, MDD);
                            if (rowsAffected > 0)
                            {
                                Directory.CreateDirectory(readFolderPath);
                                string readFileName = fileName.Replace($"\\mac_para", $"\\mac_para\\read");
                                //moving file
                                File.Move(fileName, readFileName);
                            }
                            Console.WriteLine(rowsAffected);
                    }

                }
                //}

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                

            }
        }

        void read_MAC_File(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    MacPara macParaData = new MacPara();
                    macParaData = reader.ReadClass<MacPara>();

                    MDD.Machine_Name = macParaData.Machine_Name;
                    MDD.Machine_Serial_Number = macParaData.Machine_Serial_Number;
                    MDD.Machine_Configuration = macParaData.Machine_Configuration;
                    MDD.Mould_ID = macParaData.Mould_ID;
                    MDD.Part_Number = macParaData.Part_Number;
                    MDD.Material_Name = macParaData.Material_Name;
                    MDD.Machine_Mode = macParaData.Machine_Mode.ToString();
                    MDD.Cavity_Count = macParaData.Cavity_Count.ToString();
                    MDD.Pump_Status = macParaData.Pump_Status.ToString();
                    MDD.Heater_Status = macParaData.Heater_Status.ToString();
                    MDD.Shot_Weight = macParaData.Shot_Weight.ToString();
                    MDD.Total_Good_Parts_Produced = macParaData.Total_Good_Parts_Produced.ToString();
                    MDD.Total_Rejects = macParaData.Total_Rejects.ToString();
                    MDD.Total_Good_Shots = macParaData.Total_Good_Shots.ToString();
                    MDD.Total_Shots = macParaData.Total_Shots.ToString();
                    MDD.Bin_Good_Parts_Produced = macParaData.Bin_Good_Parts_Produced.ToString();
                    MDD.Bin_Rejects = macParaData.Bin_Rejects.ToString();
                    MDD.Bin_Good_Shots = macParaData.Bin_Good_Shots.ToString();
                    MDD.Bin_Total_Shots = macParaData.Bin_Total_Shots.ToString();
                    MDD.Set_Good_Parts = macParaData.Set_Good_Parts.ToString();
                    MDD.Energy_Value = macParaData.Energy_Value.ToString();
                    MDD.Ideal_Cycle_Time = macParaData.Ideal_Cycle_Time.ToString();

                }
            }
        }
    }




    [StructLayout(LayoutKind.Sequential)]
    public class MacPara
    {
        /// <summary>
        /// 50 bytes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string Machine_Name;


        /// <summary>
        /// 16 bytes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Machine_Serial_Number;

        /// <summary>
        /// 16 bytes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string Machine_Configuration;

        /// <summary>
        /// 16 bytes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Mould_ID;

        /// <summary>
        /// 16 bytes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Part_Number;

        /// <summary>
        /// 16 bytes
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Material_Name;


        public ushort Machine_Mode;
        public ushort Cavity_Count;
        public ushort Pump_Status;
        public ushort Heater_Status;

        public float Shot_Weight;

        public uint Total_Good_Parts_Produced;

        public uint Total_Rejects;
        public uint Total_Good_Shots;
        public uint Total_Shots;
        public uint Bin_Good_Parts_Produced;
        public uint Bin_Rejects;
        public uint Bin_Good_Shots;
        public uint Bin_Total_Shots;
        public uint Set_Good_Parts;
        public int Energy_Value;
        public float Ideal_Cycle_Time;

    }

    internal class Machine_Detail_Data
    {
        public decimal NID { get; set; }
        public string Machine_Name { get; set; }
        public string Machine_Serial_Number { get; set; }
        public string Machine_Configuration { get; set; }
        public string Mould_ID { get; set; }
        public string Part_Number { get; set; }
        public string Material_Name { get; set; }
        public string Machine_Mode { get; set; }
        public string Cavity_Count { get; set; }
        public string Pump_Status { get; set; }
        public string Heater_Status { get; set; }
        public string Shot_Weight { get; set; }
        public string Total_Good_Parts_Produced { get; set; }
        public string Total_Rejects { get; set; }
        public string Total_Good_Shots { get; set; }
        public string Total_Shots { get; set; }
        public string Bin_Good_Parts_Produced { get; set; }
        public string Bin_Rejects { get; set; }
        public string Bin_Good_Shots { get; set; }
        public string Bin_Total_Shots { get; set; }
        public string Set_Good_Parts { get; set; }
        public string Energy_Value { get; set; }        
        public string Ideal_Cycle_Time { get; set; }

        public string Machine_Id { get; set; }
        public DateTime ProdDate { get; set; }
        public string ShiftName { get; set; }
    }


}
