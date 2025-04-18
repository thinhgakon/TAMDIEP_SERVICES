﻿using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_CANRA_1.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using NDTan;
using XHTD_SERVICES_CANRA_1.Devices;
using XHTD_SERVICES_CANRA_1.Business;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_CANRA_1.Jobs;

namespace XHTD_SERVICES_CANRA_1
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
            builder.RegisterType<LongVehicleRepository>().AsSelf();
            builder.RegisterType<ScaleOperatingRepository>().AsSelf();
            builder.RegisterType<SystemParameterRepository>().AsSelf();
            builder.RegisterType<PLCBarrier>().AsSelf();
            builder.RegisterType<TCPTrafficLight>().AsSelf();
            builder.RegisterType<Sensor>().AsSelf();
            builder.RegisterType<PLC>().AsSelf();
            builder.RegisterType<Logger>().AsSelf();

            builder.RegisterType<TrafficLightControl>().AsSelf();
            builder.RegisterType<BarrierControl>().AsSelf();
            builder.RegisterType<SensorControl>().AsSelf();
            builder.RegisterType<S7SensorControl>().AsSelf();

            builder.RegisterType<ScaleBusiness>().AsSelf();
            builder.RegisterType<UnladenWeightBusiness>().AsSelf();
            builder.RegisterType<WeightBusiness>().AsSelf();
            builder.RegisterType<StepBusiness>().AsSelf();
            builder.RegisterType<OrderBusiness>().AsSelf();
            builder.RegisterType<IndexOrderBusiness>().AsSelf();
            builder.RegisterType<DesicionScaleBusiness>().AsSelf();
            builder.RegisterType<ScaleApiLib>().AsSelf();
            builder.RegisterType<VehicleBusiness>().AsSelf();
            builder.RegisterType<Notification>().AsSelf();

            builder.RegisterType<TCPLed>().AsSelf();
            builder.RegisterType<TCPLedControl>().AsSelf();

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
