using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Dynamic;
using System.Xml;

namespace ToshibaBinary2DbClassLibrary.Model
{
    public class Alarm_Data
    {


        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        List<string> alarmNames;
        List <Machine_Alarm_Data> ALM;
        Configuration CFG;

        public Alarm_Data()
        {
            
            ALM = new List <Machine_Alarm_Data>();
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);
            var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;
            string filePath;
            filePath =Path.Combine(LocalFilePath, "AlarmText.txt");

            try
            {
                 alarmNames = File.ReadAllLines(filePath).ToList<string>();               
            }

            catch (FileNotFoundException)
            {
                 logger.Warn("AlarmText.txt not found. Alarms will be logged without text descriptions.");
                 alarmNames = new List<string>();
            }
            catch (IOException ex)
            {
                
                logger.Error($"Error reading file: {ex.Message}");
            }

        }

        public void read_Alarm_Files(string Machine_ID, string LocalFilePath)
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

                    string folderPath = $"{MachineFolder}Alarm";
                    string readFolderPath = $"{MachineFolder}Alarm\\read\\";


                using (IDbConnection db = new SqlConnection(cs))
                {
                    foreach (string fileName in Directory.EnumerateFiles(folderPath, "*.alm"))
                    {
                        ALM.Clear();

                        // 1. Read file into memory (ALM list)
                        read_Alarm_File(fileName);

                        if (ALM.Count == 0)
                        {
                            // If empty, just delete or skip? Better to delete if valid but empty.
                            try { File.Delete(fileName); } catch { }
                            continue;
                        }

                        ProdDateAndShift prodDateAndShift;
                        try
                        {
                            prodDateAndShift = ProdDateAndShift.GetShiftInfo(DateTime.Now);
                        }
                        catch (Exception queryEx)
                        {
                            logger.Warn($"Error calculating Shift info: {queryEx.Message}. Using defaults.");
                            prodDateAndShift = new ProdDateAndShift { ProdDate = DateTime.Now.Date, ShiftName = "A" };
                        }

                        // 2. Set context info
                        foreach (Machine_Alarm_Data _Alarm_Data in ALM)
                        {
                            _Alarm_Data.Machine_Id = Machine_ID;
                            _Alarm_Data.ProdDate = prodDateAndShift.ProdDate;
                            _Alarm_Data.ShiftName = prodDateAndShift.ShiftName;
                        }

                        // 3. High-Watermark Check: Get latest Set_Date_Time from DB
                        DateTime? latestDbTime = null;
                        try
                        {
                            string maxQuery = "SELECT MAX(Set_Date_Time) FROM [dbo].[Alarm_Data] WHERE Machine_Id = @Machine_Id";
                            // Note: Assuming Set_Date_Time in DB is DateTime or convertible. 
                            // If it's string in DB, this might need casting, but Dapper usually handles it if column is DateTime.
                            // If column is VARCHAR, we might need CAST/CONVERT in SQL. 
                            // Safest is to try fetching.
                             var result = db.ExecuteScalar(maxQuery, new { Machine_Id = Machine_ID });
                             if(result != null && result != DBNull.Value)
                             {
                                 latestDbTime = Convert.ToDateTime(result);
                             }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Failed to fetch max alarm time for {Machine_ID}: {ex.Message}");
                        }

                        int totalInFile = ALM.Count;

                        // 4. Filter duplicates
                        if (latestDbTime.HasValue)
                        {
                            // Remove alarms that are older or equal to the latest one in DB
                            ALM.RemoveAll(x => DateTime.Parse(x.Set_Date_Time) <= latestDbTime.Value);
                        }

                        int newRecords = ALM.Count;
                        int rowsAffected = 0;

                        if (newRecords > 0)
                        {
                            string InsertMachine_Alarm = @"INSERT INTO [dbo].[Alarm_Data]
                                                        (
		                                                       [Alarm_Number]
                                                               ,[Set_Date_Time]
                                                               ,[Reset_Date_Time]
                                                               ,[Machine_Id]
                                                               ,[Alarm_Status]
                                                               ,[ProdDate]
                                                               ,[ShiftName]
		                                                       )
                                                         VALUES
                                                               (
			                                                    @Alarm_Number
                                                               ,@Set_Date_Time
                                                               ,@Reset_Date_Time
                                                               ,@Machine_Id
                                                               ,@Alarm_Status
                                                               ,@ProdDate
                                                               ,@ShiftName
		                                                  )";

                            logger.Info($"[Alarm_Data] Inserting {newRecords} NEW alarms for Machine '{Machine_ID}' (Filtered {totalInFile - newRecords} duplicates)");
                            rowsAffected = db.Execute(InsertMachine_Alarm, ALM);
                        }
                        else
                        {
                            logger.Info($"[Alarm_Data] No new alarms for Machine '{Machine_ID}'. All {totalInFile} records were duplicates.");
                        }
                        
                        // Always delete file if processed (even if 0 new records, because we successfully determined we have them all)
                        try
                        {
                            File.Delete(fileName);
                            logger.Info($"[Alarm_Data] Successfully processed and deleted: {fileName}");
                        }
                        catch (Exception delEx)
                        {
                            logger.Error($"[Alarm_Data] Processing done, but failed to delete file {fileName}: {delEx.Message}");
                        }

                        Console.WriteLine($"Alarm processing for {Machine_ID}: {newRecords} inserted out of {totalInFile}.");
                    }
                }
                //}
            }
            catch (Exception ex)
            {
                 logger.Error($"Error in read_Alarm_Files for {Machine_ID}: {ex.Message}");
                    
            }
            

            //Console.ReadLine();

        }


        
        void read_Alarm_File(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {

                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int i = 0;
                    while (reader.BaseStream.Position < reader.BaseStream.Length && i<500)
                    {

                        uint Alarm_Number = reader.ReadUInt32();

                        if (Alarm_Number == 0)
                            continue;


                        DateTime Set_Date_Time = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()).LocalDateTime;
                        DateTime Reset_Date_Time = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()).LocalDateTime;

                        string s_Set_Date_Time = Set_Date_Time.ToString("yyyy-MM-dd HH:mm:ss");
                        string s_Reset_Date_Time= Reset_Date_Time.ToString("yyyy-MM-dd HH:mm:ss");

                        if (Reset_Date_Time > DateTime.Now)
                            s_Reset_Date_Time = "Active";

                        string alarmStatus = "Unknown Alarm";
                        if (alarmNames != null && Alarm_Number < alarmNames.Count)
                        {
                            alarmStatus = alarmNames[(int)Alarm_Number];
                        }
                        else
                        {
                            alarmStatus = $"Alarm #{Alarm_Number} (Text missing)";
                        }

                        ALM.Add(new Machine_Alarm_Data { NID = 0, Alarm_Number = Alarm_Number.ToString(), Set_Date_Time = s_Set_Date_Time, Reset_Date_Time = s_Reset_Date_Time, Alarm_Status = alarmStatus });
                        i++;
                    }
                   
                }
            }

        }





        

        



    }

    

    


    internal class Machine_Alarm_Data
    {
        public decimal NID { get; set; }
        public string Alarm_Number { get; set; }
        public string Set_Date_Time { get; set; }
        public string Reset_Date_Time { get; set; }
        public string Alarm_Status { get; set; }
        public string Machine_Id { get; set; }
        public DateTime ProdDate { get; set; }
        public string ShiftName { get; set; }
    }
}
