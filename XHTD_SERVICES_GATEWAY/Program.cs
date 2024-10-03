using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_GATEWAY
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
        public static bool IsConfirmAtGatewayActive = false; // cấu hình xác thực tại cổng
        public static bool IsRequireCallVoiceActive = false; // cấu hình chỉ mở cổng sau khi đã gọi loa

        public static DateTime? LastTimeReceivedUHF = DateTime.Now;

        public static bool UHFConnected = false;

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
