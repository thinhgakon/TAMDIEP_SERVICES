﻿using System;
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

        public static List<int> scaleValues = new List<int>();
        public static bool IsScalling = false;
        public static bool IsLockingScale = false;
        public static string InProgressDeliveryCode = null;
        public static string InProgressVehicleCode = null;
        public static bool IsSensorActive = false;
        public static bool IsBarrierActive = false;

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
