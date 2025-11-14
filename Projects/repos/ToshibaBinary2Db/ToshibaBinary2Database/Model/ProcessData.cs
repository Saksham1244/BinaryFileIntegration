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

namespace ToshibaBinary2Database.Model
{
    internal class ProcessData
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        //TPDSinfoFloat PDSinfo;

        Machine_Process_Data MPD;
        public ProcessData()
        {
            //TPDSinfoFloat PDSinfo = new TPDSinfoFloat();
            //PDSinfo.ElemNum = new float[43];

            MPD = new Machine_Process_Data();

        }

        public void read_PDS_Files()
        {
            var cs = ConfigurationManager.AppSettings["ConnectionString"];
            var LocalFilePath = ConfigurationManager.AppSettings["LocalFilePath"];

            string folderPath = $"{LocalFilePath}pds_para";
            string readFolderPath = $"{LocalFilePath}pds_para//read//";

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
	                                    )";

            try
            {
                using (IDbConnection db = new SqlConnection(cs))
                {
                    read_PDS_File(fileName);
                    //int temp = PDSinfo.DatTim.tm_year + 1900;
                    //Console.WriteLine($"Date,time : {PDSinfo.DatTim.tm_mday}-{PDSinfo.DatTim.tm_mon}-{temp} {PDSinfo.DatTim.tm_hour}-{PDSinfo.DatTim.tm_min}-{PDSinfo.DatTim.tm_sec}");
                    //Console.WriteLine($"Shot count: {PDSinfo.ElemNum[0]}");
                    //Console.WriteLine($"Cycle time: {PDSinfo.ElemNum[1]}");

                    //Console.WriteLine($"ElemNumCount:{PDSinfo.ElemNum.Count()}");
                    int rowsAffected = db.Execute(InsertMachine_Process_DataTable, MPD);
                        if(rowsAffected>0)
                        {
                            Directory.CreateDirectory(readFolderPath);
                            string readFileName= fileName.Replace($"\\pds_para",$"\\pds_para\\read");
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

                    //PDSinfo.ElemNum = new float[43];
                    

                    foreach (PropertyInfo prop in MPD.GetType().GetProperties())
                    {
                        if(prop.Name != "Date_Time" && prop.Name != "NID")
                        {
                            prop.SetValue(MPD, reader.ReadSingle().ToString());

                        }
                    }

                    //for (int i = 0; i < 43; i++)
                    //{
                    //    PDSinfo.ElemNum[i] = reader.ReadSingle();
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
    }




}
