using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_TRAM481
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static List<int> scaleValues1 = new List<int>();
        public static List<int> scaleValues2 = new List<int>();
        public static bool IsScalling1 = false;
        public static bool IsScalling2 = false;

        public static bool IsLockingScale1 = false;
        public static bool IsLockingScale2 = false;

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
