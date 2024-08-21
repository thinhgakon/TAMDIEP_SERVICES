using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_GATEWAY_PRINT.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_GATEWAY_PRINT.Jobs;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES_GATEWAY_PRINT
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<PrintRepository>().AsSelf();
            builder.RegisterType<PrintLogger>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(PrintJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
