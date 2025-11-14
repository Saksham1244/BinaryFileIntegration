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

namespace ToshibaBinary2Database.Model
{
    internal class MoldMachineValidation
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        MoldMachineValidation_Data MMVD;

        public MoldMachineValidation()
        {
            MMVD=new MoldMachineValidation_Data();

        }

        public void read_MldMacVld_Files()
        {
            var cs = ConfigurationManager.AppSettings["ConnectionString"];
            var LocalFilePath = ConfigurationManager.AppSettings["LocalFilePath"];

            string folderPath = $"{LocalFilePath}mold_validation";
            string readFolderPath = $"{LocalFilePath}mold_validation//read//";

            foreach (string fileName in Directory.EnumerateFiles(folderPath, "*.vld"))
            {
                string InsertMoldMachValid_DataTable = @"INSERT INTO [dbo].[MoldMachineValidationData]
                                                                   (
		                                                           [Machine_Id]
                                                                   ,[Mold_Id]
                                                                   ,[Valid_Status]
                                                                   ,[Reserve]
		                                                           )
                                                             VALUES
                                                                   (
		                                                           @Machine_Id
                                                                   ,@Mold_Id
                                                                   ,@Valid_Status
                                                                   ,@Reserve
		                                                           )";

                try
                {
                    using (IDbConnection db = new SqlConnection(cs))
                    {
                        read_Validation_File(fileName);

                        int rowsAffected = db.Execute(InsertMoldMachValid_DataTable, MMVD);
                        if (rowsAffected > 0)
                        {
                            Directory.CreateDirectory(readFolderPath);
                            string readFileName = fileName.Replace($"\\mold_validation", $"\\mold_validation\\read");
                            //moving file
                            File.Move(fileName, readFileName);
                        }
                        Console.WriteLine(rowsAffected);
                    }

                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    Console.WriteLine(ex.Message);
                }
            }

            Console.ReadLine();
        }


        void read_Validation_File(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {

                    MldMacValid validationData = new MldMacValid();
                    validationData = reader.ReadClass<MldMacValid>();

                    MMVD.Machine_Id=validationData.Machine_Id;
                    MMVD.Mold_Id = validationData.Mold_Id;
                    MMVD.Valid_Status=validationData.Valid_Status.ToString();
                    MMVD.Reserve=validationData.Reserve.ToString();
                }

            }
        }
    }



    internal class MoldMachineValidation_Data
    {
        public decimal NID { get; set; }
        public string Machine_Id { get; set; }
        public string Mold_Id { get; set; }
        public string Valid_Status { get; set; }
        public string Reserve { get; set; }
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
}
