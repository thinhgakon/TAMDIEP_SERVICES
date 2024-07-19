using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_QUEUE_TO_GATEWAY.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<QueueToGatewayClinkerJob>().AsSelf();
            builder.RegisterType<QueueToGatewayRoiJob>().AsSelf();
            builder.RegisterType<QueueToGatewayPcb40Job>().AsSelf();
            builder.RegisterType<QueueToGatewayPcb30Job>().AsSelf();
            builder.RegisterType<QueueToGatewayLogger>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToGatewayClinkerJob).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToGatewayRoiJob).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToGatewayPcb40Job).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToGatewayPcb30Job).Assembly));
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(QueueToGatewayLogger).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
