using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml;

namespace ToshibaBinary2DbClassLibrary.Model
{

    public class Moulding_Data
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        Machine_Moulding_Data MMD;
        Configuration CFG;
        public Moulding_Data()
        {
            MMD = new Machine_Moulding_Data();
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);
        }

        public void read_Mold_Files(string Machine_ID, string LocalFilePath, bool useBigEndian = false)
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

                    string folderPath = $"{MachineFolder}molding_para";
                    string readFolderPath = $"{MachineFolder}molding_para\\read\\";
                

                    foreach (string fileName in Directory.EnumerateFiles(folderPath, "MoldingPara*"))
                    {
                        string InsertMachine_Mold_DataTable = @"INSERT INTO [dbo].[Machine_Moulding_Data]
                                                       (
                                                            [Injection_pressure_step_1]
                                                           ,[Injection_pressure_step_2]
                                                           ,[Injection_pressure_step_3]
                                                           ,[Injection_pressure_step_4]
                                                           ,[Injection_speed_step_1]
                                                           ,[Injection_speed_step_2]
                                                           ,[Injection_speed_step_3]
                                                           ,[Injection_speed_step_4]
                                                           ,[Injection_position_for_speed_1]
                                                           ,[Injection_position_for_speed_2]
                                                           ,[Injection_position_for_speed_3]
                                                           ,[Injection_position_for_speed_4]
                                                           ,[Holding_pressure_step_1]
                                                           ,[Holding_pressure_step_2]
                                                           ,[Holding_pressure_step_3]
                                                           ,[Holding_pressure_step_4]
                                                           ,[Holding_time_step_1]
                                                           ,[Holding_time_step_2]
                                                           ,[Holding_time_step_3]
                                                           ,[Holding_time_step_4]
                                                           ,[Injection_time_actual]
                                                           ,[Cooling_time_actual]
                                                           ,[Dosing_time_actual]
                                                           ,[Dosing_speed_actual]
                                                           ,[Dosing_speed_step_1]
                                                           ,[Dosing_speed_step_2]
                                                           ,[Dosing_speed_step_3]
                                                           ,[Dosing_back_pressure_step_1]
                                                           ,[Dosing_back_pressure_step_2]
                                                           ,[Dosing_back_pressure_step_3]
                                                           ,[Barrel_temperature_actual_nozzle]
                                                           ,[Barrel_temperature_actual_zone_1]
                                                           ,[Barrel_temperature_actual_zone_2]
                                                           ,[Barrel_temperature_actual_zone_3]
                                                           ,[Barrel_temperature_actual_zone_4]
                                                           ,[Barrel_temperature_actual_zone_5]
                                                           ,[Oil_temperature_actual]
                                                           ,[Hot_runner_temperature_actual_zone_1]
                                                           ,[Hot_runner_temperature_actual_zone_2]
                                                           ,[Hot_runner_temperature_actual_zone_3]
                                                           ,[Hot_runner_temperature_actual_zone_4]
                                                           ,[Hot_runner_temperature_actual_zone_5]
                                                           ,[Hot_runner_temperature_actual_zone_6]
                                                           ,[Hot_runner_temperature_actual_zone_7]
                                                           ,[Hot_runner_temperature_actual_zone_8]
                                                           ,[Hot_runner_temperature_actual_zone_9]
                                                           ,[Hot_runner_temperature_actual_zone_10]
                                                           ,[Hot_runner_temperature_actual_zone_11]
                                                           ,[Hot_runner_temperature_actual_zone_12]
                                                           ,[Cascade_injection_delay_time_1]
                                                           ,[Cascade_injection_delay_time_2]
                                                           ,[Cascade_injection_delay_time_3]
                                                           ,[Cascade_injection_delay_time_4]
                                                           ,[Cascade_injection_delay_time_5]
                                                           ,[Cascade_injection_delay_time_6]
                                                           ,[Cascade_injection_delay_time_7]
                                                           ,[Cascade_injection_delay_time_8]
                                                           ,[Machine_Id]
                                                           ,[ProdDate]
                                                           ,[ShiftName]
                                                        )
                                                 VALUES
                                                       (
                                                            @Injection_pressure_step_1
                                                           ,@Injection_pressure_step_2
                                                           ,@Injection_pressure_step_3
                                                           ,@Injection_pressure_step_4
                                                           ,@Injection_speed_step_1
                                                           ,@Injection_speed_step_2
                                                           ,@Injection_speed_step_3
                                                           ,@Injection_speed_step_4
                                                           ,@Injection_position_for_speed_1
                                                           ,@Injection_position_for_speed_2
                                                           ,@Injection_position_for_speed_3
                                                           ,@Injection_position_for_speed_4
                                                           ,@Holding_pressure_step_1
                                                           ,@Holding_pressure_step_2
                                                           ,@Holding_pressure_step_3
                                                           ,@Holding_pressure_step_4
                                                           ,@Holding_time_step_1
                                                           ,@Holding_time_step_2
                                                           ,@Holding_time_step_3
                                                           ,@Holding_time_step_4
                                                           ,@Injection_time_actual
                                                           ,@Cooling_time_actual
                                                           ,@Dosing_time_actual
                                                           ,@Dosing_speed_actual
                                                           ,@Dosing_speed_step_1
                                                           ,@Dosing_speed_step_2
                                                           ,@Dosing_speed_step_3
                                                           ,@Dosing_back_pressure_step_1
                                                           ,@Dosing_back_pressure_step_2
                                                           ,@Dosing_back_pressure_step_3
                                                           ,@Barrel_temperature_actual_nozzle
                                                           ,@Barrel_temperature_actual_zone_1
                                                           ,@Barrel_temperature_actual_zone_2
                                                           ,@Barrel_temperature_actual_zone_3
                                                           ,@Barrel_temperature_actual_zone_4
                                                           ,@Barrel_temperature_actual_zone_5
                                                           ,@Oil_temperature_actual
                                                           ,@Hot_runner_temperature_actual_zone_1
                                                           ,@Hot_runner_temperature_actual_zone_2
                                                           ,@Hot_runner_temperature_actual_zone_3
                                                           ,@Hot_runner_temperature_actual_zone_4
                                                           ,@Hot_runner_temperature_actual_zone_5
                                                           ,@Hot_runner_temperature_actual_zone_6
                                                           ,@Hot_runner_temperature_actual_zone_7
                                                           ,@Hot_runner_temperature_actual_zone_8
                                                           ,@Hot_runner_temperature_actual_zone_9
                                                           ,@Hot_runner_temperature_actual_zone_10
                                                           ,@Hot_runner_temperature_actual_zone_11
                                                           ,@Hot_runner_temperature_actual_zone_12
                                                           ,@Cascade_injection_delay_time_1
                                                           ,@Cascade_injection_delay_time_2
                                                           ,@Cascade_injection_delay_time_3
                                                           ,@Cascade_injection_delay_time_4
                                                           ,@Cascade_injection_delay_time_5
                                                           ,@Cascade_injection_delay_time_6
                                                           ,@Cascade_injection_delay_time_7
                                                           ,@Cascade_injection_delay_time_8
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
                            
                            read_Mold_File(fileName, useBigEndian);

                            //SET Machine Id , Prod Date and Shift name 
                            MMD.Machine_Id = Machine_ID;
                            MMD.ProdDate = prodDateAndShift.ProdDate;
                            MMD.ShiftName = prodDateAndShift.ShiftName;

                            int rowsAffected = db.Execute(InsertMachine_Mold_DataTable, MMD);
                            if (rowsAffected > 0)
                            {
                                Directory.CreateDirectory(readFolderPath);
                                string readFileName = fileName.Replace($"\\molding_para", $"\\molding_para\\read");
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


        void read_Mold_File(string filename, bool useBigEndian = false)
        {

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    MacMold macMoldData = new MacMold();
                    macMoldData = reader.ReadClass<MacMold>();

                    // Fix endianness for all float values if needed
                    if (useBigEndian)
                    {
                        ReverseAllFloats(macMoldData);
                    }

                    MMD.Injection_pressure_step_1 = macMoldData.Injection_pressure_step_1.ToString();
                    MMD.Injection_pressure_step_2 = macMoldData.Injection_pressure_step_2.ToString();
                    MMD.Injection_pressure_step_3 = macMoldData.Injection_pressure_step_3.ToString();
                    MMD.Injection_pressure_step_4 = macMoldData.Injection_pressure_step_4.ToString();
                    MMD.Injection_speed_step_1 = macMoldData.Injection_speed_step_1.ToString();
                    MMD.Injection_speed_step_2 = macMoldData.Injection_speed_step_2.ToString();
                    MMD.Injection_speed_step_3 = macMoldData.Injection_speed_step_3.ToString();
                    MMD.Injection_speed_step_4 = macMoldData.Injection_speed_step_4.ToString();
                    MMD.Injection_position_for_speed_1 = macMoldData.Injection_position_for_speed_1.ToString();
                    MMD.Injection_position_for_speed_2 = macMoldData.Injection_position_for_speed_2.ToString();
                    MMD.Injection_position_for_speed_3 = macMoldData.Injection_position_for_speed_3.ToString();
                    MMD.Injection_position_for_speed_4 = macMoldData.Injection_position_for_speed_4.ToString();
                    MMD.Holding_pressure_step_1 = macMoldData.Holding_pressure_step_1.ToString();
                    MMD.Holding_pressure_step_2 = macMoldData.Holding_pressure_step_2.ToString();
                    MMD.Holding_pressure_step_3 = macMoldData.Holding_pressure_step_3.ToString();
                    MMD.Holding_pressure_step_4 = macMoldData.Holding_pressure_step_4.ToString();
                    MMD.Holding_time_step_1 = macMoldData.Holding_time_step_1.ToString();
                    MMD.Holding_time_step_2 = macMoldData.Holding_time_step_2.ToString();
                    MMD.Holding_time_step_3 = macMoldData.Holding_time_step_3.ToString();
                    MMD.Holding_time_step_4 = macMoldData.Holding_time_step_4.ToString();
                    MMD.Injection_time_actual = macMoldData.Injection_time_actual.ToString();
                    MMD.Cooling_time_actual = macMoldData.Cooling_time_actual.ToString();
                    MMD.Dosing_time_actual = macMoldData.Dosing_time_actual.ToString();
                    MMD.Dosing_speed_actual = macMoldData.Dosing_speed_actual.ToString();
                    MMD.Dosing_speed_step_1 = macMoldData.Dosing_speed_step_1.ToString();
                    MMD.Dosing_speed_step_2 = macMoldData.Dosing_speed_step_2.ToString();
                    MMD.Dosing_speed_step_3 = macMoldData.Dosing_speed_step_3.ToString();
                    MMD.Dosing_back_pressure_step_1 = macMoldData.Dosing_back_pressure_step_1.ToString();
                    MMD.Dosing_back_pressure_step_2 = macMoldData.Dosing_back_pressure_step_2.ToString();
                    MMD.Dosing_back_pressure_step_3 = macMoldData.Dosing_back_pressure_step_3.ToString();
                    MMD.Barrel_temperature_actual_nozzle = macMoldData.Barrel_temperature_actual_nozzle.ToString();
                    MMD.Barrel_temperature_actual_zone_1 = macMoldData.Barrel_temperature_actual_zone_1.ToString();
                    MMD.Barrel_temperature_actual_zone_2 = macMoldData.Barrel_temperature_actual_zone_2.ToString();
                    MMD.Barrel_temperature_actual_zone_3 = macMoldData.Barrel_temperature_actual_zone_3.ToString();
                    MMD.Barrel_temperature_actual_zone_4 = macMoldData.Barrel_temperature_actual_zone_4.ToString();
                    MMD.Barrel_temperature_actual_zone_5 = macMoldData.Barrel_temperature_actual_zone_5.ToString();
                    MMD.Oil_temperature_actual = macMoldData.Oil_temperature_actual.ToString();
                    MMD.Hot_runner_temperature_actual_zone_1 = macMoldData.Hot_runner_temperature_actual_zone_1.ToString();
                    MMD.Hot_runner_temperature_actual_zone_2 = macMoldData.Hot_runner_temperature_actual_zone_2.ToString();
                    MMD.Hot_runner_temperature_actual_zone_3 = macMoldData.Hot_runner_temperature_actual_zone_3.ToString();
                    MMD.Hot_runner_temperature_actual_zone_4 = macMoldData.Hot_runner_temperature_actual_zone_4.ToString();
                    MMD.Hot_runner_temperature_actual_zone_5 = macMoldData.Hot_runner_temperature_actual_zone_5.ToString();
                    MMD.Hot_runner_temperature_actual_zone_6 = macMoldData.Hot_runner_temperature_actual_zone_6.ToString();
                    MMD.Hot_runner_temperature_actual_zone_7 = macMoldData.Hot_runner_temperature_actual_zone_7.ToString();
                    MMD.Hot_runner_temperature_actual_zone_8 = macMoldData.Hot_runner_temperature_actual_zone_8.ToString();
                    MMD.Hot_runner_temperature_actual_zone_9 = macMoldData.Hot_runner_temperature_actual_zone_9.ToString();
                    MMD.Hot_runner_temperature_actual_zone_10 = macMoldData.Hot_runner_temperature_actual_zone_10.ToString();
                    MMD.Hot_runner_temperature_actual_zone_11 = macMoldData.Hot_runner_temperature_actual_zone_11.ToString();
                    MMD.Hot_runner_temperature_actual_zone_12 = macMoldData.Hot_runner_temperature_actual_zone_12.ToString();
                    MMD.Cascade_injection_delay_time_1 = macMoldData.Cascade_injection_delay_time_1.ToString();
                    MMD.Cascade_injection_delay_time_2 = macMoldData.Cascade_injection_delay_time_2.ToString();
                    MMD.Cascade_injection_delay_time_3 = macMoldData.Cascade_injection_delay_time_3.ToString();
                    MMD.Cascade_injection_delay_time_4 = macMoldData.Cascade_injection_delay_time_4.ToString();
                    MMD.Cascade_injection_delay_time_5 = macMoldData.Cascade_injection_delay_time_5.ToString();
                    MMD.Cascade_injection_delay_time_6 = macMoldData.Cascade_injection_delay_time_6.ToString();
                    MMD.Cascade_injection_delay_time_7 = macMoldData.Cascade_injection_delay_time_7.ToString();
                    MMD.Cascade_injection_delay_time_8 = macMoldData.Cascade_injection_delay_time_8.ToString();


                }
            }
        }

        private void ReverseAllFloats(MacMold data)
        {
            // Use reflection to reverse all float fields
            foreach (var field in typeof(MacMold).GetFields())
            {
                if (field.FieldType == typeof(Single))
                {
                    float value = (float)field.GetValue(data);
                    byte[] bytes = BitConverter.GetBytes(value);
                    Array.Reverse(bytes);
                    field.SetValue(data, BitConverter.ToSingle(bytes, 0));
                  }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MacMold
    {

        public Single Injection_pressure_step_1;
        public Single Injection_pressure_step_2;
        public Single Injection_pressure_step_3;
        public Single Injection_pressure_step_4;
        public Single Injection_speed_step_1;
        public Single Injection_speed_step_2;
        public Single Injection_speed_step_3;
        public Single Injection_speed_step_4;
        public Single Injection_position_for_speed_1;
        public Single Injection_position_for_speed_2;
        public Single Injection_position_for_speed_3;
        public Single Injection_position_for_speed_4;
        public Single Holding_pressure_step_1;
        public Single Holding_pressure_step_2;
        public Single Holding_pressure_step_3;
        public Single Holding_pressure_step_4;
        public Single Holding_time_step_1;
        public Single Holding_time_step_2;
        public Single Holding_time_step_3;
        public Single Holding_time_step_4;
        public Single Injection_time_actual;
        public Single Cooling_time_actual;
        public Single Dosing_time_actual;
        public Single Dosing_speed_actual;
        public Single Dosing_speed_step_1;
        public Single Dosing_speed_step_2;
        public Single Dosing_speed_step_3;
        public Single Dosing_back_pressure_step_1;
        public Single Dosing_back_pressure_step_2;
        public Single Dosing_back_pressure_step_3;
        public Single Barrel_temperature_actual_nozzle;
        public Single Barrel_temperature_actual_zone_1;
        public Single Barrel_temperature_actual_zone_2;
        public Single Barrel_temperature_actual_zone_3;
        public Single Barrel_temperature_actual_zone_4;
        public Single Barrel_temperature_actual_zone_5;
        public Single Oil_temperature_actual;
        public Single Hot_runner_temperature_actual_zone_1;
        public Single Hot_runner_temperature_actual_zone_2;
        public Single Hot_runner_temperature_actual_zone_3;
        public Single Hot_runner_temperature_actual_zone_4;
        public Single Hot_runner_temperature_actual_zone_5;
        public Single Hot_runner_temperature_actual_zone_6;
        public Single Hot_runner_temperature_actual_zone_7;
        public Single Hot_runner_temperature_actual_zone_8;
        public Single Hot_runner_temperature_actual_zone_9;
        public Single Hot_runner_temperature_actual_zone_10;
        public Single Hot_runner_temperature_actual_zone_11;
        public Single Hot_runner_temperature_actual_zone_12;
        public Single Cascade_injection_delay_time_1;
        public Single Cascade_injection_delay_time_2;
        public Single Cascade_injection_delay_time_3;
        public Single Cascade_injection_delay_time_4;
        public Single Cascade_injection_delay_time_5;
        public Single Cascade_injection_delay_time_6;
        public Single Cascade_injection_delay_time_7;
        public Single Cascade_injection_delay_time_8;

    }

    internal class Machine_Moulding_Data
    {
        public decimal NID { get; set; }
        public string Injection_pressure_step_1 { get; set; }
        public string Injection_pressure_step_2 { get; set; }
        public string Injection_pressure_step_3 { get; set; }
        public string Injection_pressure_step_4 { get; set; }
        public string Injection_speed_step_1 { get; set; }
        public string Injection_speed_step_2 { get; set; }
        public string Injection_speed_step_3 { get; set; }
        public string Injection_speed_step_4 { get; set; }
        public string Injection_position_for_speed_1 { get; set; }
        public string Injection_position_for_speed_2 { get; set; }
        public string Injection_position_for_speed_3 { get; set; }
        public string Injection_position_for_speed_4 { get; set; }
        public string Holding_pressure_step_1 { get; set; }
        public string Holding_pressure_step_2 { get; set; }
        public string Holding_pressure_step_3 { get; set; }
        public string Holding_pressure_step_4 { get; set; }
        public string Holding_time_step_1 { get; set; }
        public string Holding_time_step_2 { get; set; }
        public string Holding_time_step_3 { get; set; }
        public string Holding_time_step_4 { get; set; }
        public string Injection_time_actual { get; set; }
        public string Cooling_time_actual { get; set; }
        public string Dosing_time_actual { get; set; }
        public string Dosing_speed_actual { get; set; }
        public string Dosing_speed_step_1 { get; set; }
        public string Dosing_speed_step_2 { get; set; }
        public string Dosing_speed_step_3 { get; set; }
        public string Dosing_back_pressure_step_1 { get; set; }
        public string Dosing_back_pressure_step_2 { get; set; }
        public string Dosing_back_pressure_step_3 { get; set; }
        public string Barrel_temperature_actual_nozzle { get; set; }
        public string Barrel_temperature_actual_zone_1 { get; set; }
        public string Barrel_temperature_actual_zone_2 { get; set; }
        public string Barrel_temperature_actual_zone_3 { get; set; }
        public string Barrel_temperature_actual_zone_4 { get; set; }
        public string Barrel_temperature_actual_zone_5 { get; set; }
        public string Oil_temperature_actual { get; set; }
        public string Hot_runner_temperature_actual_zone_1 { get; set; }
        public string Hot_runner_temperature_actual_zone_2 { get; set; }
        public string Hot_runner_temperature_actual_zone_3 { get; set; }
        public string Hot_runner_temperature_actual_zone_4 { get; set; }
        public string Hot_runner_temperature_actual_zone_5 { get; set; }
        public string Hot_runner_temperature_actual_zone_6 { get; set; }
        public string Hot_runner_temperature_actual_zone_7 { get; set; }
        public string Hot_runner_temperature_actual_zone_8 { get; set; }
        public string Hot_runner_temperature_actual_zone_9 { get; set; }
        public string Hot_runner_temperature_actual_zone_10 { get; set; }
        public string Hot_runner_temperature_actual_zone_11 { get; set; }
        public string Hot_runner_temperature_actual_zone_12 { get; set; }
        public string Cascade_injection_delay_time_1 { get; set; }
        public string Cascade_injection_delay_time_2 { get; set; }
        public string Cascade_injection_delay_time_3 { get; set; }
        public string Cascade_injection_delay_time_4 { get; set; }
        public string Cascade_injection_delay_time_5 { get; set; }
        public string Cascade_injection_delay_time_6 { get; set; }
        public string Cascade_injection_delay_time_7 { get; set; }
        public string Cascade_injection_delay_time_8 { get; set; }
        public string Machine_Id { get; set; }
        public DateTime ProdDate { get; set; }
        public string ShiftName { get; set; }
    }
}
