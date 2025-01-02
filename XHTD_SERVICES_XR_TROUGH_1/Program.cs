using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES_XR_TROUGH_1
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static bool IsLockingRfid = false;
        public static tblStoreOrderOperating CurrentOrder = new tblStoreOrderOperating();
        public static tblStoreOrderOperating PreviousOrder = new tblStoreOrderOperating();

        public static DateTime? LastTimeReceivedUHF = DateTime.Now;

        public static bool UHFConnected = false;

        public static DateTime? SendFailOpenPortLastTime = null;

        public static int CountToSendFailOpenPort = 0;

        public static Dictionary<string, int> DeviceFailCount = new Dictionary<string, int>();
        public static Dictionary<string, DateTime?> DeviceLastFailPingTime = new Dictionary<string, DateTime?>();

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
