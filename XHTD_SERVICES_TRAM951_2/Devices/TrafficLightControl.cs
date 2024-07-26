using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Common;
using log4net;

namespace XHTD_SERVICES_TRAM951_2.Devices
{
    public class TrafficLightControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_2_DGT_IN;

        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_2_DGT_OUT;

        protected readonly string SCALE_DGT_IN_URL = "192.168.13.184";

        protected readonly string SCALE_DGT_OUT_URL = "192.168.13.179";

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

            _logger.Info($"IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight(string scaleCode)
        {
            var ipAddress = GetIpAddress(scaleCode);

            _logger.Info($"IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOffGreenOnRed();
        }
    }
}
