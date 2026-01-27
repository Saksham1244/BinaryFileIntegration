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
using Dapper;
using System.Data;
using System.Data.SqlClient;

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

                HashSet<string> processedMachineIds = new HashSet<string>();

                int machineIndex = 0;
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    string Machine_ID = node.Attributes["Machine_ID"].Value;

                    if (processedMachineIds.Contains(Machine_ID))
                    {
                        logger.Warn($"Duplicate configuration found for Machine {Machine_ID}. Skipping additional polling thread.");
                        Console.WriteLine($"WARNING: Duplicate Machine_ID {Machine_ID} detected. Skipping.");
                        continue;
                    }
                    processedMachineIds.Add(Machine_ID);

                    int currentIndex = machineIndex; // Capture for closure
                    machineIndex++;

                    Task.Run(async () =>
                    {
                        
                        // Fixed polling interval of 60 seconds
                        int tacTime = 60;




                        logger.Info($"Starting polling for Machine {Machine_ID} with TacTime: {tacTime}s.");
                        
                        string lastShiftName = "";
                        DateTime lastProdDate = DateTime.MinValue;

                        while (true)
                        {
                            try
                            {
                                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting processing for Machine: {Machine_ID}");

                                // Sync current shift info to database ONLY if it changed
                                ProdDateAndShift currentShift = ProdDateAndShift.GetShiftInfo(DateTime.Now);
                                if (currentShift.ShiftName != lastShiftName || currentShift.ProdDate != lastProdDate)
                                {
                                    UpdateShiftInDb(Machine_ID); 
                                    lastShiftName = currentShift.ShiftName;
                                    lastProdDate = currentShift.ProdDate;
                                }

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
                                        {
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
                                        // Process MAC files first to ensure Total_Shots is updated for the Performance SP
                                        Machine_Data md = new Machine_Data();
                                        bool macProcessed = md.read_MAC_Files(Machine_ID, LocalFilePath);

                                        // Process PDS files second. SP is triggered per-shot inside read_PDS_Files.
                                        ProcessData pd = new ProcessData();
                                        bool pdsProcessed = pd.read_PDS_Files(Machine_ID, LocalFilePath);

                                        Moulding_Data mld = new Moulding_Data();
                                        mld.read_Mold_Files(Machine_ID, LocalFilePath);

                                        Alarm_Data alarm = new Alarm_Data();
                                        alarm.read_Alarm_Files(Machine_ID, LocalFilePath);

                                        MoldMachineValidation mmv = new MoldMachineValidation();
                                        mmv.read_MldMacVld_Files(Machine_ID, LocalFilePath);

                                        if (macProcessed || pdsProcessed)
                                        {
                                            Performance_CycleTime PC = new Performance_CycleTime();
                                            PC.InsertPerformanceData(Machine_ID);
                                        }
                                    });
                                }


                            }
                            catch (Exception ex)
                            {
                                logger.Error($"Error processing machine {Machine_ID}: {ex.Message}");
                            }

                            // Wait for TacTime before next poll
                                await Task.Delay(TimeSpan.FromSeconds(tacTime));
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


        private void UpdateShiftInDb(string Machine_ID)
        {
            try
            {
                var cs = CFG.AppSettings.Settings["ConnectionString"].Value;
                ProdDateAndShift currentShift = ProdDateAndShift.GetShiftInfo(DateTime.Now);

                using (IDbConnection db = new SqlConnection(cs))
                {
                    string updateSql = @"
                        UPDATE Prod_ShiftInformation 
                        SET ProdDate = @ProdDate, 
                            ShiftName = @ShiftName 
                        WHERE StationID = (SELECT StationID FROM Config_Equipment WHERE EquipmentID = @EquipmentID)";

                    int rows = db.Execute(updateSql, new
                    {
                        ProdDate = currentShift.ProdDate,
                        ShiftName = currentShift.ShiftName,
                        EquipmentID = Machine_ID
                    });

                    if (rows > 0)
                    {
                        logger.Info($"Updated Prod_ShiftInformation for Machine {Machine_ID}: {currentShift.ShiftName} / {currentShift.ProdDate:yyyy-MM-dd}");
                    }
                    else
                    {
                        logger.Warn($"No record found in Prod_ShiftInformation for Machine {Machine_ID} (Station mapping might be missing)");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to update shift info in DB for Machine {Machine_ID}: {ex.Message}");
            }
        }
    }
}
