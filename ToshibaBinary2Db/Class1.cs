using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToshibaBinary2Db
{
    public class Class1
    {
        const string formatter = "{0,5}{1,17}{2,18:E7}";

        // Convert four byte array elements to a float and display it.
        public static void BAToSingle(byte[] bytes, int index)
        {
            float value = BitConverter.ToSingle(bytes, index);

            if (index > 4)
            {
                Console.WriteLine(formatter, index, BitConverter.ToString(bytes, index, 4), value.ToString());

            }

            else if (index == 0)
            {

                byte tm_sec = bytes[0];
                byte tm_min = bytes[1];
                byte tm_hour = bytes[2];
                byte tm_mday = bytes[3];
                byte tm_mon = bytes[4];
                byte tm_year = bytes[5];
                byte Reserve1 = bytes[6];
                byte Reserve2 = bytes[7];

                string originalDate = tm_year + 1900 + "-" + tm_mon + "-" + tm_mday + " " + tm_hour + ":" + tm_min + ":" + tm_sec;

                Console.WriteLine(formatter, index,
                BitConverter.ToString(bytes, index, 8), originalDate);

            }

            else if (index == 4)
            {

                DateTime originalDate = DateTime.FromBinary((long)value);
                Console.WriteLine(formatter, index,
                BitConverter.ToString(bytes, index, 4), originalDate.ToString("HH-mm-ss"));

            }
        }

        // Display a byte array, using multiple lines if necessary.
        public static void WriteMultiLineByteArray(byte[] bytes)
        {
            const int rowSize = 20;
            int iter;

            Console.WriteLine("initial byte array");
            Console.WriteLine("------------------");

            for (iter = 0; iter < bytes.Length - rowSize; iter += rowSize)
            {
                Console.Write(
                    BitConverter.ToString(bytes, iter, rowSize));
                Console.WriteLine("-");
            }

            Console.WriteLine(BitConverter.ToString(bytes, iter));
            Console.WriteLine();
        }

        public static void example()
        {

            string fileName = "C:\\Users\\lenovo\\Downloads\\PDSData_20240207183749.pds";

            //BinaryReader binReader = new BinaryReader(File.Open(fileName, FileMode.Open));

            byte[] byteArray = File.ReadAllBytes(fileName);

            //foreach (byte s in byteArray)
            //{
            //    // Printing the binary array value of 
            //    // the file contents 
            //    Console.WriteLine(s);
            //}

            Console.WriteLine(
            "This example of the BitConverter.ToSingle( byte( ), " +
            "int ) \nmethod generates the following output. It " +
            "converts elements \nof a byte array to float values.\n");

            WriteMultiLineByteArray(byteArray);

            Console.WriteLine(formatter, "index", "array elements",
                "float");
            Console.WriteLine(formatter, "-----", "--------------",
                "-----");

            // Convert byte array elements to float values.
            BAToSingle(byteArray, 0);

            BAToSingle(byteArray, 8);
            BAToSingle(byteArray, 12);
            BAToSingle(byteArray, 16);
            BAToSingle(byteArray, 20);
            BAToSingle(byteArray, 24);
            BAToSingle(byteArray, 28);
            BAToSingle(byteArray, 32);
            BAToSingle(byteArray, 36);
            BAToSingle(byteArray, 40);
            BAToSingle(byteArray, 44);
            BAToSingle(byteArray, 48);
            BAToSingle(byteArray, 52);
            BAToSingle(byteArray, 56);
            BAToSingle(byteArray, 60);
            BAToSingle(byteArray, 64);
            BAToSingle(byteArray, 68);
            BAToSingle(byteArray, 72);
            BAToSingle(byteArray, 76);
            BAToSingle(byteArray, 80);
            BAToSingle(byteArray, 84);
            BAToSingle(byteArray, 88);
            BAToSingle(byteArray, 92);
            BAToSingle(byteArray, 96);
            BAToSingle(byteArray, 100);
            BAToSingle(byteArray, 104);
            BAToSingle(byteArray, 108);
            BAToSingle(byteArray, 112);
            BAToSingle(byteArray, 116);
            BAToSingle(byteArray, 120);
            BAToSingle(byteArray, 124);
            BAToSingle(byteArray, 128);



            //long binaryDateValue = binReader.ReadInt64();
            //DateTime originalDate = DateTime.FromBinary(binaryDateValue);
            //Console.WriteLine($"Original date: {originalDate}");

            //double binaryDateValue = binReader.ReadDouble();
            //DateTime originalDate = DateTime.FromBinary((long)binaryDateValue);
            //Console.WriteLine($"Original date: {originalDate}");
            // Console.WriteLine($"ShotValue: {ShotValue}");











            //while (binReader.BaseStream.Position != binReader.BaseStream.Length)
            //{
            //    try
            //    { 
            //        j = binReader.ReadBytes(4).ToString();
            //        Console.WriteLine(i + " : " + j + ", Position:" + binReader.BaseStream.Position + ", Length:" + binReader.BaseStream.Length);


            //    }
            //    catch(Exception ex)
            //    {
            //        Console.WriteLine("There has been an error");
            //    }

            //    i++;


            //}
        }

        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace("-", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

    }
}
