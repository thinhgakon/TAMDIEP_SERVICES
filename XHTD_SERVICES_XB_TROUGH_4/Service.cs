﻿using Autofac;
using log4net;
using System.ServiceProcess;
using XHTD_SERVICES_XB_TROUGH_4.Hubs;
using XHTD_SERVICES_XB_TROUGH_4.Schedules;

namespace XHTD_SERVICES_XB_TROUGH_4
{
    public partial class Service : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Info("OnStart service XB TROUGH 4");
            Autofac.IContainer container = DIBootstrapper.Init();
            container.Resolve<JobScheduler>().Start();

            new SignalRService().OnStart(null);
        }

        protected override void OnStop()
        {
            log.Info("OnStop service XB TROUGH 4");
        }
    }
}
