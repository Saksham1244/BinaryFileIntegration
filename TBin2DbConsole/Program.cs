

using System;
using ToshibaBinary2DbClassLibrary.Model;
using System.Threading.Tasks;

namespace TBin2DbConsole
{
    

    internal class Program
    {
             

          
        static async Task Main(string[] args)
        {
            Machine2LocalFileTransfer mac2LocFilTrans = new ToshibaBinary2DbClassLibrary.Model.Machine2LocalFileTransfer();

            await mac2LocFilTrans.TransferBinaryFiles();

            Console.WriteLine("Transfer completed. Exiting...");
            Environment.Exit(0);
        }

        
    }
}
