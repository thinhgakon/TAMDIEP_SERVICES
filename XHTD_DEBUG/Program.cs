using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using Autofac;
using Autofac.Extras.Quartz;
using System.Collections.Specialized;
using log4net;
using XHTD_SERVICES_GATEWAY;
using XHTD_SERVICES_GATEWAY.Schedules;

namespace XHTD_DEBUG
{ 
    internal class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {            
            IContainer container = DIBootstrapper.Init();
            container.Resolve<JobScheduler>().Start();

            Console.ReadLine();
        }
    }
}
