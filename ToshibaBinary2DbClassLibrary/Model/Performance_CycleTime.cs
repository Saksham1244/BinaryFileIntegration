using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NLog;
using System.Configuration;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Data.SqlClient;
using System.Data;


namespace ToshibaBinary2DbClassLibrary.Model
{
    public class Performance_CycleTime
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        Configuration CFG;

        public Performance_CycleTime()
        {
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);
        }

        public void InsertPerformanceData(string Machine_ID)
        {
            var cs = CFG.AppSettings.Settings["ConnectionString"].Value;
            

                    string Perf_CycleTime_Insert = "Perf_CycleTime_Insert";

                    using (IDbConnection db = new SqlConnection(cs))
                    {
                        try 
                        {
                            int rowsAffected = db.Execute(Perf_CycleTime_Insert, new { Machine_Id = Machine_ID }, commandType: CommandType.StoredProcedure);
                            Console.WriteLine($"[Performance] Updated for {Machine_ID}. Rows: {rowsAffected}");
                            logger.Info($"[Performance] Updated for {Machine_ID}. Rows: {rowsAffected}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Performance] Error for {Machine_ID}: {ex.Message}");
                            logger.Error($"[Performance] Error for {Machine_ID}: {ex.Message}");
                        }
                    }
                }

    }
}
