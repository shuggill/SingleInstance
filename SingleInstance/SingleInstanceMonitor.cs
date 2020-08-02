using log4net;
using log4net.Config;
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
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SingleInstance
{
    public partial class SingleInstanceMonitor : ServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string AppSettingsProcessesToLimit = "ProcessesToLimit";
        private const string AppSettingsDelayInMs = "DelayTimeMs";
        private const int DefaultDelayInMs = 1000;

        private readonly System.Timers.Timer timer = new System.Timers.Timer();

        public SingleInstanceMonitor()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Load Log4Net config.

            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            XmlConfigurator.Configure(new FileInfo(Path.Combine(assemblyFolder, "log4net.config")));

            Logger.Info("Service Starting Up");

            var delayConfig = ConfigurationManager.AppSettings[AppSettingsDelayInMs];
            var delayInMs = DefaultDelayInMs;
            if (String.IsNullOrEmpty(delayConfig))
            {
                Logger.Warn($"Config parameter {AppSettingsDelayInMs} not set in app.config - assuming default");
            }
            else
            {
                if (!int.TryParse(delayConfig, out delayInMs))
                {
                    delayInMs = DefaultDelayInMs;
                }
            }

            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = delayInMs;
            timer.Enabled = true;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            // A bit crude, but support two modes where config is either hardcoded (to avoid tampering),
            // or configurable from app.config.

#if LOCKEDDOWN
            var processesConfig = "notepad.exe";
#else
            // Extract configuration.
            var processesConfig = ConfigurationManager.AppSettings[AppSettingsProcessesToLimit];
            if (String.IsNullOrEmpty(processesConfig))
            {
                Logger.Fatal($"Config parameter {AppSettingsProcessesToLimit} not set in app.config");
                Environment.Exit(0);
            }
#endif

            var processesToLimit = processesConfig.Split(',');

            foreach (var processToLimit in processesToLimit)
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processToLimit));
                if (processes.Length > 1)
                {
                    Logger.Warn($"Found >1 instance of {processToLimit}, attempting to kill, start time {processes[1].StartTime}");

                    // This attempts to ensure that the most recent instance is killed, not the original one.
                    processes.OrderByDescending(p => p.StartTime).First().Kill();

                    // TODO - try and wait until the process is killed before moving on.
                }
            }
        }

        protected override void OnStop()
        {
            Logger.Info("Service Stopping");
        }
    }
}
