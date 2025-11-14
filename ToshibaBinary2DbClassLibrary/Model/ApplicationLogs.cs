using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace ToshibaBinary2DbClassLibrary.Model
{
    public static class ApplicationLogs
    {
        static StackTrace  stackTrace = new StackTrace();
        public static void WriteLog( string Message)
        {
            try
            {


                var SourceMethod = stackTrace.GetFrame(1).GetMethod();
                string SourceMethodFullname = SourceMethod.ReflectedType.FullName + "." + SourceMethod.Name;

                string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(assemblyPath);
                string path = cfg.AppSettings.Settings["LogFilePath"].Value;

                Message = SourceMethodFullname + ":" + Message;

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filepath = path + "Service_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                if (!File.Exists(filepath))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
