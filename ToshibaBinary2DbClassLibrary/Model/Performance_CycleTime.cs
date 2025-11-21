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

        public void InsertPerformanceData()
        {
            var cs = CFG.AppSettings.Settings["ConnectionString"].Value;
            var LocalFilePath = CFG.AppSettings.Settings["LocalFilePath"].Value;
            var MachConfigFilePath = CFG.AppSettings.Settings["MachConfigFilePath"].Value;

            //open the XML File having the Machine configuration
            XmlDocument doc = new XmlDocument();
            doc.Load(MachConfigFilePath);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {

                string ftpAddress = node.Attributes["Machine_IP"].Value;
                string filePathOnFtp = node.Attributes["Machine_Ftp_Path"].Value;
                string username = node.Attributes["Machine_Ftp_ID"].Value;
                string password = node.Attributes["Machine_Ftp_Pwd"].Value;

                string Machine_ID = node.Attributes["Machine_ID"].Value;

                

                    string Perf_CycleTime_Insert = "Perf_CycleTime_Insert";

                    using (IDbConnection db = new SqlConnection(cs))
                    {

                        int rowsAffected = db.Execute(Perf_CycleTime_Insert, new { Machine_Id = Machine_ID }, commandType: CommandType.StoredProcedure);
                    }
                
            }

        }

    }
}
