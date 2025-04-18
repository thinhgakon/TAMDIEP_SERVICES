﻿using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_GATEWAY_OUT.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_GATEWAY_OUT.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device.PLCS71200;
using XHTD_SERVICES.Device;
using NDTan;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_GATEWAY_OUT.Business;
using XHTD_SERVICES_GATEWAY_OUT.Devices;

namespace XHTD_SERVICES_GATEWAY_OUT
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
            builder.RegisterType<SystemParameterRepository>().AsSelf();
            builder.RegisterType<PLCBarrier>().AsSelf();
            builder.RegisterType<TCPTrafficLight>().AsSelf();
            builder.RegisterType<Notification>().AsSelf();
            builder.RegisterType<PLC>().AsSelf();
            builder.RegisterType<GatewayLogger>().AsSelf();
            builder.RegisterType<ScaleApiLib>().AsSelf();
            builder.RegisterType<BarrierControl>().AsSelf();
            builder.RegisterType<S71200Control>().AsSelf();
            builder.RegisterType<AttachmentRepository>().AsSelf();
            builder.RegisterType<CheckInOutRepository>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(GatewayModuleJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
