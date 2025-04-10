﻿using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_CONFIRM.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_CONFIRM.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using NDTan;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_CONFIRM.Business;
using XHTD_SERVICES_CONFIRM.Devices;

namespace XHTD_SERVICES_CONFIRM
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
            builder.RegisterType<ConfirmLogger>().AsSelf();
            builder.RegisterType<SaleOrdersApiLib>().AsSelf();
            builder.RegisterType<BarrierControl>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(ConfirmModuleJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
