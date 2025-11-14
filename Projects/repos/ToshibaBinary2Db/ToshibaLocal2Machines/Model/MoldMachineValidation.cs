using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ToshibaLocal2Machines.Model;

namespace ToshibaLocal2Machines.Model
{
    internal class MoldMachineValidation
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        MldMacValid MMV;

        public MoldMachineValidation(string Machine_Id,string Mold_Id,int Valid_Status, int Reserve)
        {
            //byte[] Machine_Id_Bytes = Encoding.ASCII.GetBytes(Machine_Id);
            //byte[] Mold_Id_Bytes = Encoding.ASCII.GetBytes(Mold_Id);

            MMV = new MldMacValid { Machine_Id = Machine_Id, Mold_Id= Mold_Id, Valid_Status= Valid_Status, Reserve = Reserve };
        }

        public string DownloadValidationFile()
        {
            var LocalFilePath = ConfigurationManager.AppSettings["LocalFilePath"];
            string ValidationFile_Name = LocalFilePath + "mold_validation\\" + "MldMacValid" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".vld";

            try
            {
                byte[] ValidationFile_Bytes = BinaryReaderExtensions.WriteByteArray(MMV);
                System.IO.BinaryWriter binWriter = new System.IO.BinaryWriter(System.IO.File.Open(ValidationFile_Name, System.IO.FileMode.Create));
                binWriter.Flush();
                binWriter.Write(ValidationFile_Bytes); // just feed it the contents verbatim
                binWriter.Close();

                return ValidationFile_Name;
            } catch(Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
                return null;
            }
            

        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MldMacValid
    {
       
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string Machine_Id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Mold_Id;

        public int Valid_Status;
        public int Reserve;

    }
}
