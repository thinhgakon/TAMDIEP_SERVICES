using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_QUEUE_TO_CALL.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_QUEUE_TO_CALL.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_QUEUE_TO_CALL
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<VehicleRepository>().AsSelf();
            builder.RegisterType<Notification>().AsSelf();
            builder.RegisterType<QueueToCallLogger>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToCallJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
