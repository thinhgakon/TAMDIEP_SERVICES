﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_GATEWAY_OUT
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static bool IsLockingRfidIn = false;
        public static bool IsLockingRfidOut = false;
        public static DateTime? SendSmsLastTime = null;
        public static bool IsCapturing = false;

        public static bool IsBarrierOpen = false;
        public static bool IsFirstTimeChange = false;

        public static bool IsBarrierActive = false; // cấu hình tự động mở barrier

        public static DateTime? LastTimeReceivedUHF = DateTime.Now;

        public static bool UHFConnected = false;

        public static DateTime? SendFailPingLastTime = null;

        public static int CountToSendFailPing = 0;

        public static DateTime? SendFailOpenPortLastTime = null;

        public static int CountToSendFailOpenPort = 0;

        public static string CurrentVehicleInGateway = string.Empty;
        public static DateTime? LastTimeValidVehicle = null;

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
