﻿using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_QUEUE_TO_TROUGH.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_QUEUE_TO_TROUGH.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_QUEUE_TO_TROUGH
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<TroughRepository>().AsSelf();
            builder.RegisterType<CallToTroughRepository>().AsSelf();
            builder.RegisterType<SystemParameterRepository>().AsSelf();
            builder.RegisterType<QueueToTroughLogger>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToTroughPcb30Job).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToTroughPcb40Job).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToTroughC91Job).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToTroughOtherJob).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToTroughClinkerJob).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToTroughJumboJob).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToTroughRoiJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
