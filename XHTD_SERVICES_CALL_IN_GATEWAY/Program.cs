using RoundRobin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_CALL_IN_GATEWAY
{
    internal static class Program
    {
        public static readonly RoundRobinList<string> roundRobinList = new RoundRobinList<string>(
                    new List<string>{
                        "PCB30", "PCB40", "C91", "ROI", "CLINKER", "OTHER"
                    }
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
