using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Common;
using log4net;

namespace XHTD_SERVICES_TRAM951_1.Devices
{
    public class TrafficLightControl
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_2_DGT_IN;

        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_2_DGT_OUT;

        protected readonly string SCALE_DGT_IN_URL = "10.0.9.11";

        protected readonly string SCALE_DGT_OUT_URL = "10.0.9.12";

        public TrafficLightControl(
            TCPTrafficLight trafficLight
            )
        {
            _trafficLight = trafficLight;
        }

        public string GetIpAddress(string scaleCode)
        {
            var ipAddress = SCALE_DGT_IN_URL;

            if (scaleCode == SCALE_DGT_IN_CODE)
            {
                ipAddress = SCALE_DGT_IN_URL;
            }
            else if (scaleCode == SCALE_DGT_OUT_CODE)
            {
                ipAddress = SCALE_DGT_OUT_URL;
            }

            return ipAddress;
        }

        public bool TurnOnGreenTrafficLight(string scaleCode)
        {
            var ipAddress = GetIpAddress(scaleCode);

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight(string scaleCode)
        {
            var ipAddress = GetIpAddress(scaleCode);

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOffGreenOnRed();
        }
    }
}
