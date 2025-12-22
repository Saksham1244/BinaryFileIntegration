

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

            mac2LocFilTrans.StartPolling();

            // Keep the console open to allow background polling to continue
            Console.WriteLine("Polling started. Press Enter to exit...");
            Console.ReadLine();
            
            //Environment.Exit(0);
        }

        
    }
}
