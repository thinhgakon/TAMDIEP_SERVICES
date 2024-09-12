using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_CANRA_2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static List<int> scaleValues = new List<int>();
        public static List<int> scaleValuesForResetLight = new List<int>();

        public static bool IsScalling = false;
        public static bool IsLockingScale = false;
        public static string InProgressDeliveryCode = null;
        public static string InProgressVehicleCode = null;
        public static bool IsSensorActive = false;
        public static bool IsBarrierActive = false;
        public static bool IsFirstTimeResetTrafficLight = true;
        public static bool IsFirstTimeResetLed = true;

        public static bool IsLockingRfid = false;
        public static bool IsEnabledRfid = false;
        public static DateTime? EnabledRfidTime = null;

        public static string PegasusIP1 = "192.168.13.187";
        public static string PegasusIP2 = "192.168.13.187";
        public static int RefPort1 = 6000;
        public static byte RefComAdr1 = 0xFF;
        public static int RefPort2 = 6000;
        public static byte RefComAdr2 = 0xFF;

        public static DateTime? LastTimeReceivedUHF = DateTime.Now;
        public static DateTime? LastTimeReceivedScaleSocket = null;

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
