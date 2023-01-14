﻿using Autofac;
using log4net;
using System.ServiceProcess;
using XHTD_SERVICES_AUTO_REINDEX.Schedules;

namespace XHTD_SERVICES_AUTO_REINDEX
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
            log.Info("OnStart service SYNC_ORDER");
            Autofac.IContainer container = DIBootstrapper.Init();
            container.Resolve<JobScheduler>().Start();
        }

        protected override void OnStop()
        {
            log.Info("OnStop service SYNC_ORDER");
        }
    }
}
