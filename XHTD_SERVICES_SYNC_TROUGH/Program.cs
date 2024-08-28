using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_SYNC_TROUGH
{
    public static class Program
    {
        public static bool Machine12Running = false;
        public static bool SyncTrough12Running = false;
        public static bool Machine34Running = false;
        public static bool SyncTrough34Running = false;

        public static DateTime? LastTimeReceivedScaleSocket = null;

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
