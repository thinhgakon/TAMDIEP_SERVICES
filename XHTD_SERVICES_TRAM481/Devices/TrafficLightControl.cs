using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES_TRAM481.Devices
{
    public class TrafficLightControl
    {
        protected readonly TCPTrafficLight _trafficLight;

        public TrafficLightControl(
            TCPTrafficLight trafficLight
            )
        {
            _trafficLight = trafficLight;
        }

        public string GetIpAddress(string scaleCode)
        {
            var ipAddress = "10.0.20.3";

            if (scaleCode == ScaleCode.CODE_SCALE_481_DGT_IN)
            {
                ipAddress = "10.0.20.3";
            }
            else if (scaleCode == ScaleCode.CODE_SCALE_481_DGT_OUT)
            {
                ipAddress = "10.0.20.4";
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
