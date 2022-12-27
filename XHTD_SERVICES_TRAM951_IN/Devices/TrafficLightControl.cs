using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;

namespace XHTD_SERVICES_TRAM951_IN.Devices
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

        public bool TurnOnGreenTrafficLight(string luong)
        {
            _trafficLight.Connect($"ipAddress");

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight(string luong)
        {
            _trafficLight.Connect($"ipAddress");

            return _trafficLight.TurnOffGreenOnRed();
        }
    }
}
