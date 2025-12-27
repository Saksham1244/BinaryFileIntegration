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

        public void InsertPerformanceData(string Machine_ID, string shotCount)
        {
            var cs = CFG.AppSettings.Settings["ConnectionString"].Value;
            
            string Perf_CycleTime_Insert = "Perf_CycleTime_Insert";

            using (IDbConnection db = new SqlConnection(cs))
            {
                try
                {
                    int rowsAffected = db.Execute(Perf_CycleTime_Insert, new { Machine_Id = Machine_ID }, commandType: CommandType.StoredProcedure);
                }
                catch (Exception ex)
                {
                   logger.Error($"Error executing Perf_CycleTime_Insert for {Machine_ID} (Shot: {shotCount}): {ex.Message}");
                }
            }
        }

    }
}
