using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_TRAM951_2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static List<int> scaleValues481 = new List<int>();
        public static bool IsScalling481 = false;
        public static bool IsLockingScale481 = false;

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
