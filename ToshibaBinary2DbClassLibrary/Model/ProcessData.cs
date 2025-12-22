using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NLog;
using System.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

namespace ToshibaBinary2DbClassLibrary.Model
{
    public class ProcessData
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        
        Machine_Process_Data MPD;
        Configuration CFG;
        public ProcessData()
        {
            //TPDSinfoFloat PDSinfo = new TPDSinfoFloat();
            //PDSinfo.ElemNum = new float[43];

            MPD = new Machine_Process_Data();
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG= ConfigurationManager.OpenExeConfiguration(assemblyPath);

        }

        public void read_PDS_Files(string Machine_ID, string LocalFilePath, int tacTime)
        {
            try { 
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

                    string folderPath = $"{MachineFolder}pds_para";
                    string readFolderPath = $"{MachineFolder}pds_para\\read\\";

                    using (IDbConnection db = new SqlConnection(cs))
                    {
                        foreach (string fileName in Directory.EnumerateFiles(folderPath, "*.pds"))
                        {

                            //string fileName = "C:\\Users\\lenovo\\Downloads\\PDSData_20240207183749.pds";

                            string InsertMachine_Process_DataTable = @"INSERT INTO [dbo].[Machine_Process_Data]
                                        (
			                                [Date_Time]
                                           ,[Shot_Count]
                                           ,[Cycle_Time]
                                           ,[Injection_Time]
                                           ,[Dosing_Time]
                                           ,[Dosing_Stop]
                                           ,[Melt_Cushion]
                                           ,[Switch_Over_Position]
                                           ,[Mold_Close_Time]
                                           ,[Mold_Open_Time]
                                           ,[Zone_3_Temperature]
                                           ,[Nozzle_1_Temeprature]
                                           ,[Feed_Temperature]
                                           ,[Zone_1_Temeprature]
                                           ,[Zone_2_Temeprature]
                                           ,[Zone_4_Temeprature]
                                           ,[Oil_Temperature]
                                           ,[Melt_Temperature]
                                           ,[Nozzle_2_Temperaturee]
                                           ,[Switch_Over_Pressure]
                                           ,[Tonnage]
                                           ,[Mold_Open_Stop]
                                           ,[Tonnage_Build_Time]
                                           ,[Tonnage_Release_Time]
                                           ,[Ejector_Forward_Time]
                                           ,[Ejector_Back_Time]
                                           ,[Minimum_Melt_Cushion]
                                           ,[Injection_Start_Position]
                                           ,[Peak_Injection_Pressure]
                                           ,[Mold_Zone1_Temperature]
                                           ,[Mold_Zone2_Temperature]
                                           ,[MTC_Temperature]
                                           ,[Machine_Id]
                                           ,[ProdDate]
                                           ,[ShiftName]
                                           ,[Downtime]
	                                    )
                                         VALUES
                                        (
			                                @Date_Time
			                                ,@Shot_Count
			                                ,@Cycle_Time
			                                ,@Injection_Time
			                                ,@Dosing_Time
			                                ,@Dosing_Stop
			                                ,@Melt_Cushion
			                                ,@Switch_Over_Position
			                                ,@Mold_Close_Time
			                                ,@Mold_Open_Time
			                                ,@Zone_3_Temperature
			                                ,@Nozzle_1_Temeprature
			                                ,@Feed_Temperature
			                                ,@Zone_1_Temeprature
			                                ,@Zone_2_Temeprature
			                                ,@Zone_4_Temeprature
			                                ,@Oil_Temperature
			                                ,@Melt_Temperature
			                                ,@Nozzle_2_Temperaturee
			                                ,@Switch_Over_Pressure
			                                ,@Tonnage
			                                ,@Mold_Open_Stop
			                                ,@Tonnage_Build_Time
			                                ,@Tonnage_Release_Time
			                                ,@Ejector_Forward_Time
			                                ,@Ejector_Back_Time
			                                ,@Minimum_Melt_Cushion
			                                ,@Injection_Start_Position
			                                ,@Peak_Injection_Pressure
			                                ,@Mold_Zone1_Temperature
			                                ,@Mold_Zone2_Temperature
			                                ,@MTC_Temperature
                                            ,@Machine_Id
                                            ,@ProdDate
                                            ,@ShiftName
                                            ,@Downtime
	                                    )";


                            string getProdDateShift = @"select ProdDate,ShiftName from Prod_ShiftInformation 
                                                where StationID=(
                                                select StationID from Config_Equipment where EquipmentID=@EquipmentID)";
                            ProdDateAndShift prodDateAndShift = new ProdDateAndShift();

                            logger.Info($"Querying ProdDate and ShiftName for Machine {Machine_ID}");
                            try
                            {
                                prodDateAndShift = db.QueryFirst<ProdDateAndShift>(getProdDateShift, new { EquipmentID = Machine_ID });
                                logger.Info($"Successfully retrieved ProdDate: {prodDateAndShift.ProdDate}, Shift: {prodDateAndShift.ShiftName}");
                            }
                            catch (Exception queryEx)
                            {
                                logger.Warn($"Could not find ProdDate/Shift for Machine {Machine_ID}: {queryEx.Message}");
                                Console.WriteLine($"WARNING: Could not find ProdDate/Shift for Machine {Machine_ID}, using defaults");
                                // Set defaults if query fails
                                prodDateAndShift.ProdDate = DateTime.Now.Date;
                                prodDateAndShift.ShiftName = "A";
                            }

                            read_PDS_File(fileName);

                            // --- Calculate Downtime (T2 - T1 - TacTime) ---
                            double downtime = 0;
                            try
                            {
                                string getPreviousShotTime = "SELECT TOP 1 Date_Time FROM Machine_Process_Data WHERE Machine_Id = @Machine_Id ORDER BY Date_Time DESC";
                                string lastShotTimeStr = db.QueryFirstOrDefault<string>(getPreviousShotTime, new { Machine_Id = Machine_ID });

                                if (!string.IsNullOrEmpty(lastShotTimeStr))
                                {
                                    DateTime T2 = DateTime.ParseExact(MPD.Date_Time, "d-M-yyyy H-m-s", null);
                                    DateTime T1 = DateTime.ParseExact(lastShotTimeStr, "d-M-yyyy H-m-s", null);

                                    double gapSeconds = (T2 - T1).TotalSeconds;
                                    downtime = gapSeconds - tacTime;
                                    if (downtime < 1) downtime = 0;

                                    logger.Info($"Downtime calculation for {Machine_ID}: T2({T2}) - T1({T1}) - TacTime({tacTime}) = {downtime}s");
                                }
                            }
                            catch (Exception dtEx)
                            {
                                logger.Warn($"Could not calculate downtime for {Machine_ID}: {dtEx.Message}");
                            }

                            //SET Machine Id , Prod Date, Shift name and Downtime
                            MPD.Machine_Id = Machine_ID;
                            MPD.ProdDate = prodDateAndShift.ProdDate;
                            MPD.ShiftName = prodDateAndShift.ShiftName;
                            MPD.Downtime = downtime.ToString();

                            // --- Duplicate Check ---
                            string checkDuplicate = "SELECT COUNT(1) FROM [dbo].[Machine_Process_Data] WHERE [Machine_Id] = @Machine_Id AND [Date_Time] = @Date_Time AND [Shot_Count] = @Shot_Count";
                            int existingCount = db.ExecuteScalar<int>(checkDuplicate, MPD);

                            bool proceedToMove = false;
                            if (existingCount == 0)
                            {
                                logger.Info($"Inserting process data for Machine {Machine_ID}, File: {Path.GetFileName(fileName)}");
                                int rowsAffected = db.Execute(InsertMachine_Process_DataTable, MPD);
                                logger.Info($"Rows affected: {rowsAffected}");
                                if (rowsAffected > 0) proceedToMove = true;
                            }
                            else
                            {
                                logger.Warn($"Duplicate record detected for Machine {Machine_ID}, DateTime {MPD.Date_Time}, Shot {MPD.Shot_Count}. Skipping insertion.");
                                proceedToMove = true; // Still move the file as it's already in the DB
                            }

                            if (proceedToMove)
                            {
                                try
                                {
                                    Directory.CreateDirectory(readFolderPath);
                                    string readFileName = fileName.Replace($"\\pds_para", $"\\pds_para\\read");
                                    
                                    if (File.Exists(readFileName))
                                    {
                                        logger.Warn($"File {Path.GetFileName(readFileName)} already exists in read folder. Deleting source file.");
                                        File.Delete(fileName);
                                    }
                                    else
                                    {
                                        File.Move(fileName, readFileName);
                                        logger.Info($"Successfully moved file: {Path.GetFileName(fileName)}");
                                    }
                                    Console.WriteLine($"✓ Processed {Path.GetFileName(fileName)} for Machine {Machine_ID}");
                                }
                                catch (Exception fileEx)
                                {
                                    logger.Error($"Failed to handle file {fileName}: {fileEx.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"WARNING: Failed to process {Path.GetFileName(fileName)} for Machine {Machine_ID}");
                            }
                            //Console.WriteLine(rowsAffected);

                        }
                    }


                //}




            }
            catch (Exception ex)
            {
                logger.Error($"Error in read_PDS_Files for Machine {Machine_ID}: {ex.Message}");
                logger.Error($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"ERROR in ProcessData for Machine {Machine_ID}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    logger.Error($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }





        }

        //void read_PDS_File(string filename, ref TPDSinfoFloat data)
        void read_PDS_File(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    //Machine_Process_Data mpd = new Machine_Process_Data();
                    TDatTim DatTim=new TDatTim();
                    DatTim= reader.ReadStruct<TDatTim>();
                    //PDSinfo.DatTim = reader.ReadStruct<TDatTim>();
                    //int temp = PDSinfo.DatTim.tm_year + 1900;

                    int temp= DatTim.tm_year + 1900;

                    // mpd.Date_Time = $"{PDSinfo.DatTim.tm_mday}-{PDSinfo.DatTim.tm_mon}-{temp} {PDSinfo.DatTim.tm_hour}-{PDSinfo.DatTim.tm_min}-{PDSinfo.DatTim.tm_sec}";

                    MPD.Date_Time = $"{DatTim.tm_mday}-{DatTim.tm_mon}-{temp} {DatTim.tm_hour}-{DatTim.tm_min}-{DatTim.tm_sec}";


                    MPD.Shot_Count = reader.ReadSingle().ToString();
                    MPD.Cycle_Time = reader.ReadSingle().ToString();
                    MPD.Injection_Time = reader.ReadSingle().ToString();
                    MPD.Dosing_Time = reader.ReadSingle().ToString();
                    MPD.Dosing_Stop = reader.ReadSingle().ToString();
                    MPD.Melt_Cushion = reader.ReadSingle().ToString();
                    MPD.Switch_Over_Position = reader.ReadSingle().ToString();
                    MPD.Mold_Close_Time = reader.ReadSingle().ToString();
                    MPD.Mold_Open_Time = reader.ReadSingle().ToString();
                    MPD.Zone_3_Temperature = reader.ReadSingle().ToString();
                    MPD.Nozzle_1_Temeprature = reader.ReadSingle().ToString();
                    MPD.Feed_Temperature = reader.ReadSingle().ToString();
                    MPD.Zone_1_Temeprature = reader.ReadSingle().ToString();
                    MPD.Zone_2_Temeprature = reader.ReadSingle().ToString();
                    MPD.Zone_4_Temeprature = reader.ReadSingle().ToString();
                    MPD.Oil_Temperature = reader.ReadSingle().ToString();
                    MPD.Melt_Temperature = reader.ReadSingle().ToString();
                    MPD.Nozzle_2_Temperaturee = reader.ReadSingle().ToString();
                    MPD.Switch_Over_Pressure = reader.ReadSingle().ToString();
                    MPD.Tonnage = reader.ReadSingle().ToString();
                    MPD.Mold_Open_Stop = reader.ReadSingle().ToString();
                    MPD.Tonnage_Build_Time = reader.ReadSingle().ToString();
                    MPD.Tonnage_Release_Time = reader.ReadSingle().ToString();
                    MPD.Ejector_Forward_Time = reader.ReadSingle().ToString();
                    MPD.Ejector_Back_Time = reader.ReadSingle().ToString();
                    MPD.Minimum_Melt_Cushion = reader.ReadSingle().ToString();
                    MPD.Injection_Start_Position = reader.ReadSingle().ToString();
                    MPD.Peak_Injection_Pressure = reader.ReadSingle().ToString();
                    MPD.Mold_Zone1_Temperature = reader.ReadSingle().ToString();
                    MPD.Mold_Zone2_Temperature = reader.ReadSingle().ToString();
                    MPD.MTC_Temperature = reader.ReadSingle().ToString();



                    //foreach (PropertyInfo prop in MPD.GetType().GetProperties())
                    //{
                    //    if(prop.Name != "Date_Time" && prop.Name != "NID")
                    //    {
                    //        prop.SetValue(MPD, reader.ReadSingle().ToString());

                    //    }
                    //}


                }
            }
        }

    }

    
    struct TDatTim
    {
        public byte tm_sec;
        public byte tm_min;
        public byte tm_hour;
        public byte tm_mday;
        public byte tm_mon;
        public byte tm_year;
        public byte Reserve1;
        public byte Reserve2;
    }

    struct TPDSinfoFloat
    {
        public TDatTim DatTim;
        public float[] ElemNum;
    }

    

    internal class Machine_Process_Data
    {
        public decimal NID { get; set; }
        public string Date_Time { get; set; }
        public string Shot_Count { get; set; }
        public string Cycle_Time { get; set; }
        public string Injection_Time { get; set; }
        public string Dosing_Time { get; set; }
        public string Dosing_Stop { get; set; }
        public string Melt_Cushion { get; set; }
        public string Switch_Over_Position { get; set; }
        public string Mold_Close_Time { get; set; }
        public string Mold_Open_Time { get; set; }
        public string Zone_3_Temperature { get; set; }
        public string Nozzle_1_Temeprature { get; set; }
        public string Feed_Temperature { get; set; }
        public string Zone_1_Temeprature { get; set; }
        public string Zone_2_Temeprature { get; set; }
        public string Zone_4_Temeprature { get; set; }
        public string Oil_Temperature { get; set; }
        public string Melt_Temperature { get; set; }
        public string Nozzle_2_Temperaturee { get; set; }
        public string Switch_Over_Pressure { get; set; }
        public string Tonnage { get; set; }
        public string Mold_Open_Stop { get; set; }
        public string Tonnage_Build_Time { get; set; }
        public string Tonnage_Release_Time { get; set; }
        public string Ejector_Forward_Time { get; set; }
        public string Ejector_Back_Time { get; set; }
        public string Minimum_Melt_Cushion { get; set; }
        public string Injection_Start_Position { get; set; }
        public string Peak_Injection_Pressure { get; set; }
        public string Mold_Zone1_Temperature { get; set; }
        public string Mold_Zone2_Temperature { get; set; }
        public string MTC_Temperature { get; set; }

        public string Machine_Id { get; set; }
        public DateTime ProdDate { get; set; }
        public string ShiftName { get; set; }
        public string Downtime { get; set; }


    }




}
