using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using RoundRobin;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY
{
    internal static class Program
    {
        public static readonly RoundRobinList<string> roundRobinList = new RoundRobinList<string>(
            new XHTD_Entities().tblTypeProducts
                .Where(x => x.State == true)
                .Select(x => x.Code)
                .ToList()
        );

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
