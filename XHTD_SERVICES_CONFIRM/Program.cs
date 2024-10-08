﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_CONFIRM
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static bool IsLockingRfid = false;

        public static DateTime? LastTimeReceivedUHF = DateTime.Now;

        public static bool UHFConnected = false;

        public static DateTime? SendFailPingLastTime = null;

        public static int CountToSendFailPing = 0;

        public static DateTime? SendFailOpenPortLastTime = null;

        public static int CountToSendFailOpenPort = 0;

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
