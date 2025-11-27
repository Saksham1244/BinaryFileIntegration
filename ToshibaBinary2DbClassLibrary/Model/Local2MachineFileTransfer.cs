using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WinSCP;

namespace ToshibaBinary2DbClassLibrary.Model
{
    public class Local2MachineFileTransfer
    {
        //private static Logger logger = LogManager.GetCurrentClassLogger();

        MldMacValid mldMacValid;
        public Local2MachineFileTransfer(MldMacValid _mldMacValid)
        {
            mldMacValid = _mldMacValid;
        }

        public void TransferBinaryFiles()
        {
            try
            {


                // var MachConfigFilePath = ConfigurationManager.AppSettings["MachConfigFilePath"];
                var MachConfigFilePath = @"D:\ToshibaIntegrationTesting\ConfigurationFile\MachineConfiguration.xml";
            //open the XML File having the Machine configuration
            XmlDocument doc = new XmlDocument();
            doc.Load(MachConfigFilePath);

            //XmlNodeList nodelist = doc.SelectNodes($"//MachineDetails[Machine_ID='{Machine_ID}']");
            XmlNodeList nodelist = doc.SelectNodes($"//MachineDetails[@Machine_ID='{mldMacValid.Machine_Id}']");

                Console.WriteLine(MachConfigFilePath);

                foreach (XmlNode node in nodelist)
            {

                string ftpAddress = node.Attributes["Machine_IP"].Value;
                string filePathOnFtp = node.Attributes["Machine_Ftp_Path"].Value;
                string username = node.Attributes["Machine_Ftp_ID"].Value;
                string password = node.Attributes["Machine_Ftp_Pwd"].Value;
                
                

                    Console.WriteLine(ftpAddress);
                    Console.WriteLine(filePathOnFtp);

                    //Testing setting ftpAddress to local Host
                    //ftpAddress = "localhost";
                    //filePathOnFtp = "upload";
                    string FtpPath = $@"{filePathOnFtp}/mold_validation/";
                    //string FtpPath = filePathOnFtp;
                    // Setup session options    
                    Console.WriteLine(FtpPath);

                    MoldMachineValidation machValidation=new MoldMachineValidation(mldMacValid);
                    string ValidationFile_Path = machValidation.DownloadValidationFile(mldMacValid.Machine_Id);
                    Console.WriteLine(ValidationFile_Path);

                    if (string.IsNullOrEmpty(ValidationFile_Path))
                    {
                        Console.WriteLine($"ValidationFile_Path is null or empty for Machine_Id: {mldMacValid.Machine_Id}. Skipping transfer.");
                        continue;
                    }

                    SessionOptions sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Ftp,
                        HostName = ftpAddress,
                        UserName = username,
                        Password = password
                        //,                            SshHostKeyFing
                    };

                    using (Session session = new Session())
                    {
                        // Connect
                        session.Open(sessionOptions);

                        if (!session.FileExists(FtpPath))
                        {
                            // Create directory if it does not exist
                            session.CreateDirectory(FtpPath);
                            Console.WriteLine("Folder Created");
                        }

                        // Upload files
                        TransferOptions transferOptions = new TransferOptions();
                        transferOptions.TransferMode = TransferMode.Binary;

                        TransferOperationResult transferResult;
                        transferResult =
                            session.PutFiles(ValidationFile_Path, FtpPath, false, transferOptions);

                        // Throw on any error
                        transferResult.Check();

                        // Print results
                        foreach (TransferEventArgs transfer in transferResult.Transfers)
                        {
                            Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
                        }
                    }              

            }

            }
            catch (Exception ex)
            {
                //logger.Error(ex.Message);
                Console.WriteLine (ex.ToString());
                ApplicationLogs.WriteLog(ex.Message);
                
            }

}



    }
}
