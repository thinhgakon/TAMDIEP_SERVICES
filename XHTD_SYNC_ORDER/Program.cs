using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using Autofac;
using XHTD_SYNC_ORDER.Jobs;
using XHTD_SYNC_ORDER.Schedules;
using Autofac.Extras.Quartz;
using System.Collections.Specialized;
using log4net;

namespace XHTD_SYNC_ORDER
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
