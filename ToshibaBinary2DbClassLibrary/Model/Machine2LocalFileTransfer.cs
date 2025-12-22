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
using System.Net.NetworkInformation;


namespace ToshibaBinary2DbClassLibrary.Model
{
    public class Machine2LocalFileTransfer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        Configuration CFG;
        public void StartPolling()
        {
            try
            {
                string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
                CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);

                var MachConfigFilePath = CFG.AppSettings.Settings["MachConfigFilePath"].Value;
                var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;
                var ClearSourceFileOnDownload = CFG.AppSettings.Settings["ClearSourceFileOnDownload"].Value;

                bool _ClearSourceFileOnDownload = (ClearSourceFileOnDownload == "1" || ClearSourceFileOnDownload.Equals("true", StringComparison.OrdinalIgnoreCase));

                //open the XML File having the Machine configuration
                XmlDocument doc = new XmlDocument();
                doc.Load(MachConfigFilePath);


                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    Task.Run(async () =>
                    {
                        // Get TacTime from XML, default to 60 seconds if missing or invalid
                        int tacTime = 60;
                        if (node.Attributes["TacTime"] != null)
                        {
                            int.TryParse(node.Attributes["TacTime"].Value, out tacTime);
                        }
                        logger.Info($"Starting polling for Machine {node.Attributes["Machine_ID"].Value} with TacTime: {tacTime} seconds.");

                        while (true)
                        {
                            try
                            {
                                string Machine_ID = node.Attributes["Machine_ID"].Value;
                                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting processing for Machine: {Machine_ID}");

                                string ftpAddress = node.Attributes["Machine_IP"].Value;

                                // --- Ping Check ---
                                try
                                {
                                    using (Ping ping = new Ping())
                                    {
                                        PingReply reply = ping.Send(ftpAddress, 2000); // 2 second timeout
                                        if (reply.Status != IPStatus.Success)
                                        {
                                            logger.Warn($"Ping failed for Machine {Machine_ID} ({ftpAddress}). Status: {reply.Status}. Skipping this cycle.");
                                            Console.WriteLine($"WARNING: Machine {Machine_ID} is unreachable. Skipping.");
                                            await Task.Delay(tacTime * 1000);
                                            continue;
                                        }
                                    }
                                }
                                catch (Exception pingEx)
                                {
                                    logger.Error($"Ping error for Machine {Machine_ID}: {pingEx.Message}. Skipping.");
                                    await Task.Delay(tacTime * 1000);
                                    continue;
                                }
                                // ------------------
                                string filePathOnFtp = node.Attributes["Machine_Ftp_Path"].Value;
                                string username = node.Attributes["Machine_Ftp_ID"].Value;
                                string password = node.Attributes["Machine_Ftp_Pwd"].Value;

                                string MachineFolder = LocalFilePath + "\\" + Machine_ID + "\\";

                                //Create directory if not exists
                                Directory.CreateDirectory(MachineFolder);

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
                                    Timeout = TimeSpan.FromSeconds(30)
                                };

                                using (Session session = new Session())
                                {
                                    // Connect
                                    session.Open(sessionOptions);

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
                                        logger.Info($"Download of {transferResult.Transfers.Count} files succeeded for {Machine_ID}.");
                                        
                                        if (_ClearSourceFileOnDownload)
                                        {
                                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                                            {
                                                try
                                                {
                                                    RemovalOperationResult removalResult = session.RemoveFiles(transfer.FileName);
                                                    removalResult.Check();
                                                }
                                                catch (Exception ex)
                                                {
                                                    logger.Error($"Failed to delete remote file {transfer.FileName}: {ex.Message}");
                                                }
                                            }
                                        }
                                    }
                                }

                                await Task.Run(() =>
                                {
                                    ProcessData pd = new ProcessData();
                                    pd.read_PDS_Files(Machine_ID, LocalFilePath, tacTime);

                                    Machine_Data md = new Machine_Data();
                                    md.read_MAC_Files(Machine_ID, LocalFilePath);

                                    Moulding_Data mld = new Moulding_Data();
                                    mld.read_Mold_Files(Machine_ID, LocalFilePath);

                                    Alarm_Data alarm = new Alarm_Data();
                                    alarm.read_Alarm_Files(Machine_ID, LocalFilePath);

                                    MoldMachineValidation mmv = new MoldMachineValidation();
                                    mmv.read_MldMacVld_Files(Machine_ID, LocalFilePath);

                                    // Run the stored proc to performance tables
                                    Performance_CycleTime PC = new Performance_CycleTime();
                                    PC.InsertPerformanceData();
                                });

                                // --- Local File Cleanup ---
                                try
                                {
                                    if (Directory.Exists(MachineFolder))
                                    {
                                        string[] localFiles = Directory.GetFiles(MachineFolder);
                                        if (localFiles.Length > 0)
                                        {
                                            logger.Info($"Cleaning up {localFiles.Length} local files for {Machine_ID} after processing.");
                                            foreach (string file in localFiles)
                                            {
                                                try
                                                {
                                                    File.Delete(file);
                                                }
                                                catch (Exception delEx)
                                                {
                                                    logger.Error($"Failed to delete local file {file}: {delEx.Message}");
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception cleanupEx)
                                {
                                    logger.Error($"Error during local file cleanup for {Machine_ID}: {cleanupEx.Message}");
                                }
                                // ---------------------------
                            }
                            catch (Exception ex)
                            {
                                logger.Error($"Error processing machine {node.Attributes["Machine_ID"]?.Value}: {ex.Message}");
                            }

                            // Wait for TacTime before next poll
                            await Task.Delay(tacTime * 1000);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

}
