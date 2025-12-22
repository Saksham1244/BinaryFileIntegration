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
            Console.WriteLine("Starting Toshiba Machine Data Polling Service...");
            
            Machine2LocalFileTransfer mac2LocFilTrans = new ToshibaBinary2DbClassLibrary.Model.Machine2LocalFileTransfer();
            mac2LocFilTrans.StartPolling();


            // Keep the application running
            Console.WriteLine("Polling started. Press Enter to exit...");
            Console.ReadLine();

        }
    }
}
