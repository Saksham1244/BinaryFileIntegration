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
            
            // Launch the polling loops for all machines
            // This runs asynchronously in background tasks
            Machine2LocalFileTransfer mac2LocFilTrans = new ToshibaBinary2DbClassLibrary.Model.Machine2LocalFileTransfer();
            mac2LocFilTrans.StartPolling();
        }

        protected override void OnStop()
        {
            logger.Info("Service is stopped at " + DateTime.Now);
        }

       
    }
}
