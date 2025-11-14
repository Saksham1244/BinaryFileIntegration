using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using ToshibaBinary2DbClassLibrary.Model;
using NLog;

namespace ToshibaMacine2Local2DbWinService
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public Service1()
        {
            InitializeComponent();


        }

        protected override void OnStart(string[] args)
        {
            logger.Info("Service is started at " + DateTime.Now);
            
            Configuration CFG;
            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            CFG = ConfigurationManager.OpenExeConfiguration(assemblyPath);


            var _TimeInterval = CFG.AppSettings.Settings["ServiceTimeInterval"].Value;
            int.TryParse(_TimeInterval,out int TimeInterval);

            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = TimeInterval; //number in milisecinds
            timer.Enabled = true;


        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            Machine2LocalFileTransfer mac2LocFilTrans = new ToshibaBinary2DbClassLibrary.Model.Machine2LocalFileTransfer();

            mac2LocFilTrans.TransferBinaryFiles();

        }

        protected override void OnStop()
        {
            
            logger.Info("Service is stopped at " + DateTime.Now);
        }

       
    }
}
