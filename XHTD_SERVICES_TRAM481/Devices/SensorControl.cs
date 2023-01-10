using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;

namespace XHTD_SERVICES_TRAM481.Devices
{
    public class SensorControl
    {
        protected readonly Sensor _sensor;

        private const string IP_ADDRESS = "10.0.20.2";

        private const int SCALE_481_I1 = 4;
        private const int SCALE_481_I2 = 5;
        private const int SCALE_481_I3 = 6;
        private const int SCALE_481_I4 = 7;

        public SensorControl(
            Sensor sensor
            )
        {
            _sensor = sensor;
        }

        public bool IsInValidSensorScale481()
        {
            var connectStatus = _sensor.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return false;
            }

            var checkInScale1 = _sensor.ReadInputPort(SCALE_481_I1);
            var checkOutScale1 = _sensor.ReadInputPort(SCALE_481_I2);
            var checkLeftScale1 = _sensor.ReadInputPort(SCALE_481_I3);
            var checkRightScale1 = _sensor.ReadInputPort(SCALE_481_I4);

            if (checkInScale1 || checkOutScale1 || checkLeftScale1 || checkRightScale1)
            {
                return false;
            }

            return true;
        }

        public bool CheckValidSensor()
        {
            List<int> portNumberDeviceIns = new List<int>
            {
                1,
                2
            };

            return _sensor.CheckValid("IpAddress", 1, portNumberDeviceIns);
        }
    }
}
