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

namespace ToshibaBinary2Database.Model
{
    internal class Alarm_Data
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        Machine_Alarm_Data ALM;

        public Alarm_Data()
        {
            ALM = new Machine_Alarm_Data();
        }

        public void read_Alarm_Files()
        {

            var cs = ConfigurationManager.AppSettings["ConnectionString"];
            var LocalFilePath = ConfigurationManager.AppSettings["LocalFilePath"];

            string folderPath = $"{LocalFilePath}Alarm";
            string readFolderPath = $"{LocalFilePath}Alarm//read//";

            foreach (string fileName in Directory.EnumerateFiles(folderPath, "*.alm"))
            {
                string InsertMachine_Mold_DataTable = @"INSERT INTO [dbo].[Alarm_Data]
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


                try
                {
                    using (IDbConnection db = new SqlConnection(cs))
                    {
                        read_Alarm_File(fileName);

                        int rowsAffected = db.Execute(InsertMachine_Mold_DataTable, ALM);
                        if (rowsAffected > 0)
                        {
                            Directory.CreateDirectory(readFolderPath);
                            string readFileName = fileName.Replace($"\\Alarm", $"\\Alarm\\read");
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

        void read_Alarm_File(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {

                using (BinaryReader reader = new BinaryReader(fs))
                {

                    MacAlarm macAlarmData = new MacAlarm();
                    macAlarmData = reader.ReadClass<MacAlarm>();

                    ALM.Alarm_Number = macAlarmData.Alarm_Number.ToString();
                    ALM.Set_Date_Time= macAlarmData.Set_Date_Time.ToString();
                    ALM.Reset_Date_Time = macAlarmData.Reset_Date_Time.ToString();

                }
            }

        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MacAlarm
    {
        public ushort Alarm_Number;

       
        public TAlarmDatTime Set_Date_Time;

        
        public TAlarmDatTime Reset_Date_Time;
    }

    public class TAlarmDatTime
    {
        
        public byte tm_min;
        public byte tm_hour;
        public byte tm_mday;
        public byte tm_mon;
        public byte tm_year;
        
    }

    internal class Machine_Alarm_Data
    {
        public decimal NID { get; set; }
        public string Alarm_Number { get; set; }
        public string Set_Date_Time { get; set; }
        public string Reset_Date_Time { get; set; }
    }
}
