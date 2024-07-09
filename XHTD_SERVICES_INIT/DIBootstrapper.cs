﻿using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_INIT.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_INIT.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_INIT
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<SyncOrderLogger>().AsSelf();

            RegisterScheduler(builder);

            return builder.Build();
        }

        private static void RegisterScheduler(ContainerBuilder builder)
        {
            var schedulerConfig = new NameValueCollection {
              {"quartz.threadPool.threadCount", "20"},
              {"quartz.scheduler.threadName", "MyScheduler"}
            };

            builder.RegisterModule(new QuartzAutofacFactoryModule
            {
                ConfigurationProvider = c => schedulerConfig
            });

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(SyncInProgressOrderJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
