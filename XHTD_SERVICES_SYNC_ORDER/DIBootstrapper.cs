using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_SYNC_ORDER.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_SYNC_ORDER.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_SYNC_ORDER.Business;

namespace XHTD_SERVICES_SYNC_ORDER
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<VehicleRepository>().AsSelf();
            builder.RegisterType<CallToTroughRepository>().AsSelf();
            builder.RegisterType<SystemParameterRepository>().AsSelf();
            builder.RegisterType<Notification>().AsSelf();
            builder.RegisterType<SyncOrderLogger>().AsSelf();
            builder.RegisterType<MachineRepository>().AsSelf();
            builder.RegisterType<TroughRepository>().AsSelf();
            builder.RegisterType<MachineApiLib>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(SyncOrderJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
