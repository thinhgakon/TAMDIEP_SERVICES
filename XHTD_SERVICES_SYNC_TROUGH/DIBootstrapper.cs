﻿using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_SYNC_TROUGH.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_SYNC_TROUGH.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_SYNC_TROUGH.Leds;

namespace XHTD_SERVICES_SYNC_TROUGH
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<MachineRepository>().AsSelf();
            builder.RegisterType<TroughRepository>().AsSelf();
            builder.RegisterType<CallToTroughRepository>().AsSelf();
            builder.RegisterType<SystemParameterRepository>().AsSelf();
            builder.RegisterType<Notification>().AsSelf();
            builder.RegisterType<SyncTroughLogger>().AsSelf();
            builder.RegisterType<MachineRepository>().AsSelf();
            builder.RegisterType<TCPLedControl>().AsSelf();
            builder.RegisterType<TCPLed>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(SyncTroughJob34).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
