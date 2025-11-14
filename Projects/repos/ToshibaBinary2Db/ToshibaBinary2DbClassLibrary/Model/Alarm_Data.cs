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

            
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);

        }

        public  async Task read_Alarm_Files()
        {

            try
            {
                var cs = CFG.AppSettings.Settings["ConnectionString"].Value;
                var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;
                var MachConfigFilePath = CFG.AppSettings.Settings["MachConfigFilePath"].Value;

                //open the XML File having the Machine configuration
                XmlDocument doc = new XmlDocument();
                doc.Load(MachConfigFilePath);

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    string ftpAddress = node.Attributes["Machine_IP"].Value;
                    string filePathOnFtp = node.Attributes["Machine_Ftp_Path"].Value;
                    string username = node.Attributes["Machine_Ftp_ID"].Value;
                    string password = node.Attributes["Machine_Ftp_Pwd"].Value;

                    string Machine_ID = node.Attributes["Machine_ID"].Value;
                    string MachineFolder = LocalFilePath + "\\" + Machine_ID + "\\";

                    string folderPath = $"{MachineFolder}Alarm";
                    string readFolderPath = $"{MachineFolder}Alarm\\read\\";

                    string AlarmTextfilePath;
                    AlarmTextfilePath = Path.Combine(MachineFolder, "AlarmText.txt");

                    try
                    {
                        alarmNames = File.ReadAllLines(AlarmTextfilePath).ToList<string>();
                    }
                    catch (FileNotFoundException fnfex)
                    {
                        logger.Error($"File not found: {fnfex.Message}");

                    }
                    catch (IOException ex)
                    {

                        logger.Error($"Error reading file: {ex.Message}");
                    }


                    foreach (string fileName in Directory.EnumerateFiles(folderPath, "*.alm"))
                    {
                        ALM = new List<Machine_Alarm_Data>();

                        string InsertMachine_Alarm = @"INSERT INTO [dbo].[Alarm_Data]
                                                    (
		                                                   [Alarm_Number]
                                                           ,[Set_Date_Time]
                                                           ,[Reset_Date_Time]
                                                           ,[Alarm_Status]
                                                           ,[Machine_Id]
                                                           ,[ProdDate]
                                                           ,[ShiftName]

		                                                   )
                                                     VALUES
                                                           (
			                                                @Alarm_Number
                                                           ,@Set_Date_Time
                                                           ,@Reset_Date_Time
                                                           ,@Alarm_Status  
                                                           ,@Machine_Id
                                                           ,@ProdDate
                                                           ,@ShiftName
		                                              )";
                        string getProdDateShift = @"select ProdDate,ShiftName from Prod_ShiftInformation 
                                                where StationID=(
                                                select StationID from Config_Equipment where EquipmentID=@EquipmentID)";

                        try { 

                        using (IDbConnection db = new SqlConnection(cs))
                        {
                            //ProdDateAndShift prodDateAndShift =  db.QueryFirstOrDefault<ProdDateAndShift>(getProdDateShift, new { EquipmentID = Machine_ID });
                            var prodDateAndShift_List= await db.QueryAsync<ProdDateAndShift>(getProdDateShift, new { EquipmentID = Machine_ID });
                            ProdDateAndShift prodDateAndShift = prodDateAndShift_List.FirstOrDefault();

                                if (prodDateAndShift == null)
                                    break;

                                read_Alarm_File(fileName);
                            //SET Machine Id , Prod Date and Shift name 
                            foreach (Machine_Alarm_Data _Alarm_Data in ALM)
                            {
                                _Alarm_Data.Machine_Id = Machine_ID;
                                _Alarm_Data.ProdDate = prodDateAndShift.ProdDate;
                                _Alarm_Data.ShiftName = prodDateAndShift.ShiftName;
                            }


                            int rowsAffected = await db.ExecuteAsync(InsertMachine_Alarm, ALM);
                            if (rowsAffected > 0)
                            {
                                Directory.CreateDirectory(readFolderPath);
                                string readFileName = fileName.Replace($"\\Alarm\\", $"\\Alarm\\read\\");
                                //moving file
                                File.Move(fileName, readFileName);
                            }
                            //Console.WriteLine(rowsAffected);
                        }
                         }
                        catch(Exception dbEx) { 
                        
                            logger.Error("Database error:" + dbEx);
                        
                        
                        }
                    }
                }
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

                        string Alarm_Status="";

                        if (alarmNames !=null)
                            Alarm_Status = alarmNames[(int)Alarm_Number];                



                        ALM.Add(new Machine_Alarm_Data { NID = 0, Alarm_Number = Alarm_Number.ToString(), Set_Date_Time = s_Set_Date_Time, Reset_Date_Time = s_Reset_Date_Time, Alarm_Status = Alarm_Status });
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
