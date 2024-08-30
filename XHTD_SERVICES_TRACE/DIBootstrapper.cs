using Autofac.Extras.Quartz;
using Autofac;
using System.Collections.Specialized;
using XHTD_SERVICES_TRACE.Schedules;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRACE.Jobs;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_TRACE
{
    public static class DIBootstrapper
    {
        public static IContainer Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<TraceLogger>().AsSelf();

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

            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(TraceServiceJob).Assembly));
            builder.RegisterType<JobScheduler>().AsSelf();
        }
    }
}
