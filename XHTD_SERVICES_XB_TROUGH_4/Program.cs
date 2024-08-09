using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_XB_TROUGH_4
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static bool IsLockingRfid = false;

        public static string PegasusIP1 = "192.168.13.161";
        public static string PegasusIP2 = "192.168.13.162";
        public static int RefPort1 = 6000;
        public static byte RefComAdr1 = 0xFF;
        public static int RefPort2 = 2000;
        public static byte RefComAdr2 = 0xFF;


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
