
using ToshibaBinary2DbClassLibrary.Model;

namespace TBin2DbConsole
{
    

    internal class Program
    {
             

          
        static void Main(string[] args)
        {
            Machine2LocalFileTransfer mac2LocFilTrans = new ToshibaBinary2DbClassLibrary.Model.Machine2LocalFileTransfer();

            mac2LocFilTrans.TransferBinaryFiles();


        }

        
    }
}
