using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WinSCP;
using NLog;
using System.Reflection;
using System.IO;


namespace ToshibaBinary2DbClassLibrary.Model
{
    public class Machine2LocalFileTransfer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        Configuration CFG;
        public async void TransferBinaryFiles()
        {
            try
            {
                string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
                CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);

                var MachConfigFilePath = CFG.AppSettings.Settings["MachConfigFilePath"].Value;                 
                var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;                 
                var ClearSourceFileOnDownload = CFG.AppSettings.Settings["ClearSourceFileOnDownload"].Value;               
                bool.TryParse(ClearSourceFileOnDownload, out bool _ClearSourceFileOnDownload);


                Console.WriteLine(MachConfigFilePath);
                Console.WriteLine(LocalFilePath);
                Console.WriteLine(ClearSourceFileOnDownload);



                //open the XML File having the Machine configuration
                XmlDocument doc = new XmlDocument();
                doc.Load(MachConfigFilePath);
                
            // List of folders to read on the FTP directory

                //List<string> DataFolders = new List<string>(new string[] { "pds_para", "mac_para", "Alarm", "molding_para" });

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    string ftpAddress = node.Attributes["Machine_IP"].Value;
                    string filePathOnFtp = node.Attributes["Machine_Ftp_Path"].Value;
                    string username = node.Attributes["Machine_Ftp_ID"].Value;
                    string password = node.Attributes["Machine_Ftp_Pwd"].Value;

                    string Machine_ID = node.Attributes["Machine_ID"].Value;
                    string MachineFolder = LocalFilePath +"\\" +Machine_ID +"\\" ;

                    //Create directory if not exists
                    Directory.CreateDirectory(MachineFolder);

                        Console.WriteLine(MachineFolder);
                    

                        Console.WriteLine(ftpAddress);
                        Console.WriteLine(filePathOnFtp);
                        Console.WriteLine(username);
                        Console.WriteLine(password);
                        //return;

                        //Testing setting ftpAddress to local Host
                        //ftpAddress = "localhost";
                        string FtpPath = $"//{filePathOnFtp}/*";
                    // Setup session options
                    SessionOptions sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Ftp,
                        HostName = ftpAddress,
                        UserName = username,
                        Password = password
                        //,                            SshHostKeyFingerprint = "ssh-rsa 2048 xxxxxxxxxxx..."
                    };

                    using (Session session = new Session())
                    {
                        // Connect
                        session.Open(sessionOptions);

                        // Download files
                        TransferOptions transferOptions = new TransferOptions();
                        transferOptions.TransferMode = TransferMode.Binary;
                        transferOptions.OverwriteMode=OverwriteMode.Overwrite;

                        TransferOperationResult transferResult;
                        transferResult =
                            session.GetFiles(FtpPath, MachineFolder, _ClearSourceFileOnDownload, transferOptions);
                        
                        // Throw on any error
                        transferResult.Check();

                        // Print results
                        //foreach (TransferEventArgs transfer in transferResult.Transfers)
                        //{
                        //    Console.WriteLine("Download of {0} succeeded", transfer.FileName);

                        //}
                        logger.Info("Download of {0} files succeeded",transferResult.Transfers.Count());
                    }

                    await Task.Run(() =>
                    {
                        ProcessData pd = new ProcessData();
                        pd.read_PDS_Files();

                        Machine_Data md = new Machine_Data();
                        md.read_MAC_Files();

                        Moulding_Data mld = new Moulding_Data();
                        mld.read_Mold_Files();

                        Alarm_Data alarm = new Alarm_Data();
                        alarm.read_Alarm_Files();

                        MoldMachineValidation mmv = new MoldMachineValidation();
                        mmv.read_MldMacVld_Files();

                        // Run the stored proc to performance tables
                        Performance_CycleTime PC = new Performance_CycleTime();
                        PC.InsertPerformanceData();
                        

                    });


                }

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine($"Error: {ex.Message}");
                //ApplicationLogs.WriteLog(ex.Message);

            }
        }
    }

}
