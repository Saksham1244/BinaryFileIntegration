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

        public bool read_PDS_Files(string Machine_ID, string LocalFilePath)
        {
            bool anyFileProcessed = false;
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
                                       ,[TimeStamp]
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
                                        ,@TimeStamp
                                    )";


                            var files = Directory.GetFiles(folderPath, "*.pds");
                            logger.Info($"[ProcessData] Found {files.Length} .pds files in {folderPath} for Machine {Machine_ID}");

                            foreach (string fileName in files)
                            {
                                // 1. Read file FIRST to get machine timestamp and shot count
                                read_PDS_File(fileName);

                                // 2. Parse machine time to determine accurate ProdDate and ShiftName
                                DateTime machineDateTime;
                                try
                                {
                                    // Use standard parsing since we now format it correctly in read_PDS_File
                                    machineDateTime = DateTime.Parse(MPD.Date_Time);
                                }
                                catch (Exception ex)
                                {
                                    logger.Error($"[ProcessData] Failed to parse machine time '{MPD.Date_Time}' for {fileName}: {ex.Message}. Falling back to now.");
                                    machineDateTime = DateTime.Now;
                                }

                                // 3. Calculate Shift based on Machine Time (Historical accuracy)
                                ProdDateAndShift prodInfo = ProdDateAndShift.GetShiftInfo(machineDateTime);
                                
                                MPD.Machine_Id = Machine_ID;
                                MPD.ProdDate = prodInfo.ProdDate.Date;
                                MPD.ShiftName = prodInfo.ShiftName;
                                MPD.TimeStamp = DateTime.Now;

                                // --- Duplicate Check (Latest Only) ---
                                // Use TRY_CAST to safely handle non-numeric data in the DB
                                string checkLastShot = "SELECT TOP 1 TRY_CAST([Shot_Count] AS decimal(18,4)) FROM [dbo].[Machine_Process_Data] WHERE [Machine_Id] = @Machine_Id ORDER BY NID DESC";
                                decimal? lastShot = db.ExecuteScalar<decimal?>(checkLastShot, new { Machine_Id = Machine_ID });
                                
                                bool isDuplicate = false;
                                if (lastShot.HasValue)
                                {
                                    isDuplicate = (Math.Abs(lastShot.Value - decimal.Parse(MPD.Shot_Count)) < 0.0001m);
                                }

                                bool proceedToMove = false;
                                if (!isDuplicate)
                                {
                                    logger.Info($"[ProcessData] Inserting NEW data for Machine '{Machine_ID}'. Shot: '{MPD.Shot_Count}', Shift Time: {machineDateTime:yyyy-MM-dd HH:mm:ss}");
                                    // Debug logging to identify the invalid value
                                    logger.Info($"[ProcessData] Machine: {Machine_ID}, Date_Time (String): '{MPD.Date_Time}', ProdDate (DateTime): '{MPD.ProdDate:yyyy-MM-dd HH:mm:ss}', Shot: '{MPD.Shot_Count}'");
                                    
                                    int rowsAffected = db.Execute(InsertMachine_Process_DataTable, MPD);
                                    if (rowsAffected > 0) 
                                    {
                                        proceedToMove = true;
                                        anyFileProcessed = true;
                                        Console.WriteLine($"[ProcessData] Inserted Shot {MPD.Shot_Count} for {Machine_ID}");

                                        // Trigger Performance SP for this SPECIFIC NEW shot
                                        //try
                                        //{
                                        //    Performance_CycleTime PC = new Performance_CycleTime();
                                        //    PC.InsertPerformanceData(Machine_ID, MPD.Shot_Count);
                                        //}
                                        //catch (Exception spEx)
                                        //{
                                        //    logger.Error($"[ProcessData] Failed to trigger Performance SP for Machine {Machine_ID}, Shot {MPD.Shot_Count}: {spEx.Message}");
                                        //}
                                    }
                                }
                                else
                                {
                                    logger.Warn($"[ProcessData] Duplicate Shot detected for Machine '{Machine_ID}'. Current File Shot: '{MPD.Shot_Count}', Database Latest: '{lastShot}'. Skipping.");
                                    proceedToMove = true; // Already exists, cleanup file
                                }

                                if (proceedToMove)
                                {
                                    try
                                    {
                                        File.Delete(fileName);
                                        logger.Info($"[ProcessData] Successfully deleted processed file: {Path.GetFileName(fileName)}");
                                    }
                                    catch (Exception fileEx)
                                    {
                                        logger.Error($"[ProcessData] Failed to delete file {fileName}: {fileEx.Message}");
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


            return anyFileProcessed;


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

                    int year= DatTim.tm_year + 1900;
                    int month = DatTim.tm_mon + 1; 

                    try 
                    {
                        DateTime dt = new DateTime(year, month, DatTim.tm_mday, DatTim.tm_hour, DatTim.tm_min, DatTim.tm_sec);
                        // Use ISO8601 format (T separator) to be safe for SQL
                        MPD.Date_Time = dt.ToString("yyyy-MM-ddTHH:mm:ss");
                    }
                    catch
                    {
                        MPD.Date_Time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    }

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

    
#pragma warning disable CS0649
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
#pragma warning restore CS0649

    

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
        public DateTime TimeStamp { get; set; }



    }




}
