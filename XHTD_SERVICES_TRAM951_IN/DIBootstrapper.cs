using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_TRAM951_IN.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951_IN.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using NDTan;

namespace XHTD_SERVICES_TRAM951_IN
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<XHTD_Entities>().AsSelf();
            builder.RegisterType<StoreOrderOperatingRepository>().AsSelf();
            builder.RegisterType<RfidRepository>().AsSelf();
            builder.RegisterType<CategoriesDevicesRepository>().AsSelf();
            builder.RegisterType<CategoriesDevicesLogRepository>().AsSelf();
            builder.RegisterType<VehicleRepository>().AsSelf();
            builder.RegisterType<PLCBarrier>().AsSelf();
            builder.RegisterType<TCPTrafficLight>().AsSelf();
            builder.RegisterType<Sensor>().AsSelf();
            builder.RegisterType<PLC>().AsSelf();
            builder.RegisterType<Tram951Logger>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(Tram951ModuleJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
