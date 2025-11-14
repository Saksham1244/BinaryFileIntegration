using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ToshibaBinary2DbClassLibrary.Model;
namespace ToshibaBinary2Database
{

    struct tAlaInfo
    {
        public uint AlaNum;
        public uint AlaOnDateTime;
        public uint AlaOffDateTime;
    }
    internal class Program
    {

        static tAlaInfo[] AlaInfo = new tAlaInfo[500];

        static void ReadAlarm(string filename, ref tAlaInfo data)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    byte[] buffer = reader.ReadBytes(Marshal.SizeOf(typeof(tAlaInfo)));

                    for (int i = 0; i < buffer.Length; i=i+3)
                    {
                        string Alarm_Number = buffer[i].ToString();

                        uint s = buffer[i+1];
                        uint r = buffer[i+2];
                        DateTime Set_Date_Time = DateTimeOffset.FromUnixTimeSeconds(s).LocalDateTime;
                        DateTime Reset_Date_Time = DateTimeOffset.FromUnixTimeSeconds(r).LocalDateTime;
                    }

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {

                        string Alarm_Number = reader.ReadUInt32().ToString();

                        uint s = reader.ReadUInt32();
                        uint r = reader.ReadUInt32();
                        DateTime Set_Date_Time = DateTimeOffset.FromUnixTimeSeconds(s).LocalDateTime;
                        DateTime Reset_Date_Time = DateTimeOffset.FromUnixTimeSeconds(r).LocalDateTime;



                    }



                }
            }
        }

        

        static void Main(string[] args)
        {


            // ProcessData pd = new ProcessData();
            // pd.read_PDS_Files();

            // Machine_Data md = new Machine_Data();
            //  md.read_MAC_Files();

            //  Moulding_Data mld = new Moulding_Data();
            //  mld.read_Mold_Files();

            Alarm_Data alarm = new Alarm_Data();
            alarm.read_Alarm_Files();

            // MoldMachineValidation mmv = new MoldMachineValidation();
            // mmv.read_MldMacVld_Files();


           // ReadAlarm(@"D:\download\Alarm\Alarm_20250828121748.alm",  ref AlaInfo[0]);



        }
    }
}
