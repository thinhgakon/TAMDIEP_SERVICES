using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Device;

namespace XHTD_SERVICES_XR_TROUGH_1.Devices
{
    public class TCPTrafficLightControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly string IP_ADDRESS = "192.168.13.233";

        public TCPTrafficLightControl(
            TCPTrafficLight trafficLight
            )
        {
            _trafficLight = trafficLight;
        }

        public bool TurnOnGreenTrafficLight()
        {
            _logger.Info($"IP đèn: {IP_ADDRESS}");

            _trafficLight.Connect(IP_ADDRESS);

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight()
        {
            _logger.Info($"IP đèn: {IP_ADDRESS}");

            _trafficLight.Connect(IP_ADDRESS);

            return _trafficLight.TurnOffGreenOnRed();
        }

        public bool TurnOffTrafficLight()
        {
            _logger.Info($"IP đèn: {IP_ADDRESS}");

            _trafficLight.Connect(IP_ADDRESS);

            return _trafficLight.TurnOffGreenOffRed();
        }
    }
}
