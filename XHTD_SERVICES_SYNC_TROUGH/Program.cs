﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_SYNC_TROUGH
{
    public static class Program
    {
        public static DateTime? LastTimeReceivedScaleSocket = DateTime.Now;

        public static Dictionary<string, int> DeviceFailCount = new Dictionary<string, int>();
        public static Dictionary<string, DateTime?> DeviceLastFailPingTime = new Dictionary<string, DateTime?>();

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
