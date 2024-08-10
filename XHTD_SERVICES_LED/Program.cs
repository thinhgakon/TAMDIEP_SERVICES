using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES_LED.Models;

namespace XHTD_SERVICES_LED
{
    internal static class Program
    {
        public static List<ScaleInfoModel> listScale1 = new List<ScaleInfoModel>();
        public static List<ScaleInfoModel> listScale2 = new List<ScaleInfoModel>();
        public static bool IsScallingCN = false;
        public static bool IsScallingCC = false;
        public static string ScalingVehicle1 = string.Empty;
        public static string ScalingVehicle2 = string.Empty;

        public static bool IsFirstTimeResetLed1 = true;

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
