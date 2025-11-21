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

                bool _ClearSourceFileOnDownload = (ClearSourceFileOnDownload == "1" || ClearSourceFileOnDownload.Equals("true", StringComparison.OrdinalIgnoreCase));


                Console.WriteLine(MachConfigFilePath);
                Console.WriteLine(LocalFilePath);
                Console.WriteLine(ClearSourceFileOnDownload);



                //open the XML File having the Machine configuration
                XmlDocument doc = new XmlDocument();
                doc.Load(MachConfigFilePath);
                
            // List of folders to read on the FTP directory

                //List<string> DataFolders = new List<string>(new string[] { "pds_para", "mac_para", "Alarm", "molding_para" });

                List<Task> machineTasks = new List<Task>();

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    machineTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            string ftpAddress = node.Attributes["Machine_IP"].Value;
                            string filePathOnFtp = node.Attributes["Machine_Ftp_Path"].Value;
                            string username = node.Attributes["Machine_Ftp_ID"].Value;
                            string password = node.Attributes["Machine_Ftp_Pwd"].Value;

                            string Machine_ID = node.Attributes["Machine_ID"].Value;
                            string MachineFolder = LocalFilePath + "\\" + Machine_ID + "\\";

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
                            
                            // Ensure filePathOnFtp does not have double slashes and ends with /*
                            string cleanPath = filePathOnFtp.TrimStart('/');
                            string FtpPath = $"/{cleanPath}/*";
                            
                            // Setup session options
                            SessionOptions sessionOptions = new SessionOptions
                            {
                                Protocol = Protocol.Ftp,
                                HostName = ftpAddress,
                                UserName = username,
                                Password = password,
                                Timeout = TimeSpan.FromSeconds(30)  // 30 second timeout
                                //,                            SshHostKeyFingerprint = "ssh-rsa 2048 xxxxxxxxxxx..."
                            };

                            using (Session session = new Session())
                            {
                                // Connect
                                session.Open(sessionOptions);

                                // Debug: List files in the directory to verify existence and path
                                try 
                                {
                                    RemoteDirectoryInfo directoryInfo = session.ListDirectory(filePathOnFtp);
                                    logger.Info($"Listing files in {filePathOnFtp}:");
                                    foreach (RemoteFileInfo fileInfo in directoryInfo.Files)
                                    {
                                        logger.Info($" - {fileInfo.Name} (IsDirectory: {fileInfo.IsDirectory})");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.Error($"Failed to list directory {filePathOnFtp}: {ex.Message}");
                                }

                                // Download files
                                TransferOptions transferOptions = new TransferOptions();
                                transferOptions.TransferMode = TransferMode.Binary;
                                transferOptions.OverwriteMode = OverwriteMode.Overwrite;

                                TransferOperationResult transferResult = null;

                                // Download files (remove=false, we will delete explicitly)
                                transferResult = session.GetFiles(FtpPath, MachineFolder, false, transferOptions);

                                // Throw on any error
                                transferResult.Check();

                                if (transferResult.Transfers.Count > 0)
                                {
                                    logger.Info($"Download of {transferResult.Transfers.Count} files succeeded.");
                                    Console.WriteLine("Files copied successfully");

                                    if (_ClearSourceFileOnDownload)
                                    {
                                        logger.Info("Attempting to delete transferred files from FTP...");
                                        foreach (TransferEventArgs transfer in transferResult.Transfers)
                                        {
                                            try
                                            {
                                                // transfer.FileName is the full remote path
                                                RemovalOperationResult removalResult = session.RemoveFiles(transfer.FileName);
                                                removalResult.Check();
                                                logger.Info($"Deleted remote file: {transfer.FileName}");
                                            }
                                            catch (Exception ex)
                                            {
                                                logger.Error($"Failed to delete remote file {transfer.FileName}: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    logger.Info("No files found to download.");
                                }
                            }

                            await Task.Run(() =>
                            {
                                ProcessData pd = new ProcessData();
                                pd.read_PDS_Files(Machine_ID, LocalFilePath);

                                Machine_Data md = new Machine_Data();
                                md.read_MAC_Files(Machine_ID, LocalFilePath);

                                Moulding_Data mld = new Moulding_Data();
                                mld.read_Mold_Files(Machine_ID, LocalFilePath);

                                Alarm_Data alarm = new Alarm_Data();
                                alarm.read_Alarm_Files(Machine_ID, LocalFilePath);

                                MoldMachineValidation mmv = new MoldMachineValidation();
                                mmv.read_MldMacVld_Files(Machine_ID, LocalFilePath);

                                // Run the stored proc to performance tables
                                // TODO: Re-enable this when Perf_CycleTime_Insert stored procedure is available
                                // Performance_CycleTime PC = new Performance_CycleTime();
                                // PC.InsertPerformanceData();


                            });
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Error processing machine {node.Attributes["Machine_ID"]?.Value}: {ex.Message}");
                            Console.WriteLine($"Error processing machine {node.Attributes["Machine_ID"]?.Value}: {ex.Message}");
                        }
                    }));
                }

                await Task.WhenAll(machineTasks);

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
