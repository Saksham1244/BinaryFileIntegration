using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NLog;
using WinSCP;

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
                        string Machine_ID = node.Attributes["Machine_ID"].Value;
                        
                        // Get TacTime from XML, default to 60 seconds if missing or invalid
                        int tacTime = 60;
                        if (node.Attributes["TacTime"] != null)
                        {
                            int.TryParse(node.Attributes["TacTime"].Value, out tacTime);
                        }



                        logger.Info($"Starting polling for Machine {Machine_ID} with TacTime: {tacTime}s.");

                        while (true)
                        {
                            try
                            {
                                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting processing for Machine: {Machine_ID}");

                                string ftpAddress = node.Attributes["Machine_IP"].Value;

                                // --- Ping Check ---
                                bool pingSuccess = false;
                                try
                                {
                                    using (Ping ping = new Ping())
                                    {
                                        PingReply reply = ping.Send(ftpAddress, 2000); // 2 second timeout
                                        if (reply.Status == IPStatus.Success)
                                        {
                                            pingSuccess = true;
                                        }
                                        else
                                        {
                                            logger.Warn($"Ping failed for Machine {Machine_ID} ({ftpAddress}). Status: {reply.Status}.");
                                            Console.WriteLine($"WARNING: Machine {Machine_ID} is unreachable. Skipping.");
                                        }
                                    }
                                }
                                catch (Exception pingEx)
                                {
                                    logger.Error($"Ping error for Machine {Machine_ID}: {pingEx.Message}.");
                                }

                                if (pingSuccess)
                                {
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


                                        if (_ClearSourceFileOnDownload && transferResult.Transfers.Count > 0)
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
                                        Performance_CycleTime PC = new Performance_CycleTime();
                                        PC.InsertPerformanceData();
                                    });
                                }


                            }
                            catch (Exception ex)
                            {
                                logger.Error($"Error processing machine {Machine_ID}: {ex.Message}");
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
