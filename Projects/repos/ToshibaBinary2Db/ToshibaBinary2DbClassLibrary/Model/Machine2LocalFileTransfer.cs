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
        public async Task TransferBinaryFiles()
        {
            try
            {
                string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
                CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);

                var MachConfigFilePath = CFG.AppSettings.Settings["MachConfigFilePath"].Value;                 
                var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;                 
                var ClearSourceFileOnDownload = CFG.AppSettings.Settings["ClearSourceFileOnDownload"].Value;

                bool _ClearSourceFileOnDownload = false;
                if (ClearSourceFileOnDownload == "1")
                    _ClearSourceFileOnDownload = true;
                


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
                        

                        try
                        {
                            session.Open(sessionOptions);
                            
                            // Download files karan sir kitna tiem 
                            TransferOptions transferOptions = new TransferOptions();
                            transferOptions.TransferMode = TransferMode.Binary;
                            transferOptions.OverwriteMode = OverwriteMode.Overwrite;

                            TransferOperationResult transferResult;
                            transferResult =
                                session.GetFiles(FtpPath, MachineFolder, false, transferOptions);

                            // Throw on any error
                            transferResult.Check();

                            if (_ClearSourceFileOnDownload)
                            {
                                // Print results
                                foreach (TransferEventArgs transfer in transferResult.Transfers)
                                {
                                    //Console.WriteLine("Download of {0} succeeded", transfer.FileName);
                                    if(transfer.FileName != "AlarmText.txt")
                                    session.RemoveFile(transfer.FileName);

                                }
                            }
                                logger.Info("Download of {0} files succeeded", transferResult.Transfers.Count());
                        }
                        catch (Exception ex) {
                            logger.Error("WinScp connection error for Machine:" + ftpAddress + "Error: " + ex);
                        }
                    }

                    

                }

                //Await feature not working as expected, 
                
                Console.WriteLine("Alarm DB transfer Started");

                Alarm_Data alarm = new Alarm_Data();
                await alarm.read_Alarm_Files();

                Console.WriteLine("Alarm DB transfer Completed");
                Console.WriteLine("Process DB transfer Started");

                ProcessData pd = new ProcessData();
                await pd.read_PDS_Files();

                Console.WriteLine("Process DB transfer Completed");
                Console.WriteLine("Machine DB transfer Started");

                Machine_Data md = new Machine_Data();
                await md.read_MAC_Files();

                Console.WriteLine("Machine DB transfer Completed");
                Console.WriteLine("Moulding DB transfer Started");

                Moulding_Data mld = new Moulding_Data();
                await mld.read_Mold_Files();

                Console.WriteLine("Moulding DB transfer Completed");
                Console.WriteLine("Mould Validation DB transfer Started");



                MoldMachineValidation mmv = new MoldMachineValidation();
               await  mmv.read_MldMacVld_Files();

                Console.WriteLine("Mould Validation DB transfer Completed");
                Console.WriteLine("Performance DB transfer Started");

                // Run the stored proc to performance tables

                Performance_CycleTime PC = new Performance_CycleTime();
               await PC.InsertPerformanceData();

                Console.WriteLine("Performancen DB transfer Completed");
                





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
