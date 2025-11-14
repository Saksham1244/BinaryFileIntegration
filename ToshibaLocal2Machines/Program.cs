
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
//using ToshibaLocal2Machines.Model;
using WinSCP;
using ToshibaBinary2DbClassLibrary.Model;

namespace ToshibaLocal2Machines
{
    internal class Program
    { 
       
        static void Main(string[] args)
        {

            MldMacValid mldMacValid= new MldMacValid();
            string Machine_Id ;
            string Mold_Id ;
            int Valid_Status ;
            int Reserve ;

            Machine_Id = "Machine1";
            Mold_Id = "1234";
            Valid_Status = Int32.Parse("1");
            

            if (args.Length == 4)
            {

                Machine_Id = args[0];
                Mold_Id = args[1];
                Valid_Status = Int32.Parse(args[2]);
                Reserve = Int32.Parse(args[3]);
            }
            else if (args.Length == 3)
            {

                Machine_Id = args[0];
                Mold_Id = args[1];
                Valid_Status = Int32.Parse(args[2]);
                Reserve = Int32.Parse("0");
            }
            else
            {
                Console.WriteLine("Please pass Machine_Id, Mold_Id No, Valid_Status");
                
                return;
            }

            mldMacValid = new MldMacValid() { Machine_Id = Machine_Id, Mold_Id = Mold_Id, Valid_Status = Valid_Status, Reserve = Reserve };
            Local2MachineFileTransfer L2M = new Local2MachineFileTransfer(mldMacValid);
            L2M.TransferBinaryFiles();


        }



    }
}
