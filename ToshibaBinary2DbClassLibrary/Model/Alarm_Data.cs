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
                        string InsertMachine_Alarm = @"INSERT INTO [dbo].[Machine_Alarm_Data]
                                                    (
		                                                   [Alarm_Number]
                                                           ,[Set_Date_Time]
                                                           ,[Reset_Date_Time]

		                                                   )
                                                     VALUES
                                                           (
			                                                @Alarm_Number
                                                           ,@Set_Date_Time
                                                           ,@Reset_Date_Time
		                                              )";
                        string getProdDateShift = @"select ProdDate,ShiftName from Prod_ShiftInformation 
                                                where StationID=(
                                                select StationID from Config_Equipment where EquipmentID=@EquipmentID)";
                        ProdDateAndShift prodDateAndShift = new ProdDateAndShift();

                        try
                        {
                            prodDateAndShift = db.QueryFirst<ProdDateAndShift>(getProdDateShift, new { EquipmentID = Machine_ID });
                            // logger.Info($"Successfully retrieved ProdDate: {prodDateAndShift.ProdDate}, Shift: {prodDateAndShift.ShiftName}");
                        }
                        catch (Exception queryEx)
                        {
                            logger.Warn($"Could not find ProdDate/Shift for Machine {Machine_ID}: {queryEx.Message}");
                            Console.WriteLine($"WARNING: Could not find ProdDate/Shift for Machine {Machine_ID}, using defaults");
                            // Set defaults if query fails
                            prodDateAndShift.ProdDate = DateTime.Now.Date;
                            prodDateAndShift.ShiftName = "A";
                        }

                        read_Alarm_File(fileName);
                        //SET Machine Id , Prod Date and Shift name 
                        foreach(Machine_Alarm_Data _Alarm_Data in ALM)
                        {
                            _Alarm_Data.Machine_Id = Machine_ID;
                            _Alarm_Data.ProdDate = prodDateAndShift.ProdDate;
                            _Alarm_Data.ShiftName = prodDateAndShift.ShiftName;
                        }
                        

                        int rowsAffected = db.Execute(InsertMachine_Alarm, ALM);
                        if (rowsAffected > 0)
                        {
                            Directory.CreateDirectory(readFolderPath);
                            string readFileName = fileName.Replace($"\\Alarm", $"\\Alarm\\read");
                            //moving file
                            File.Move(fileName, readFileName);
                        }
                        Console.WriteLine($"Alarm has been updated into the db for Machine: {Machine_ID}. Rows updated: {rowsAffected}");
                    }
                }
                //}
            }
            catch (Exception ex)
            {
                 logger.Error(ex.Message);
                    
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

                        string s_Set_Date_Time = Set_Date_Time.ToString("F");
                        string s_Reset_Date_Time= Reset_Date_Time.ToString("F");

                        if (Reset_Date_Time > DateTime.Now)
                            s_Reset_Date_Time = "ALARM IS STILL ACTIVE";

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
