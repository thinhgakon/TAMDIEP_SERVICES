using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;

namespace XHTD_SERVICES_TRAM951_IN.Devices
{
    public class SensorControl
    {
        protected readonly Sensor _sensor;

        private const string IP_ADDRESS = "10.0.9.6";

        private const int SCALE_1_I1 = 4;
        private const int SCALE_1_I2 = 5;
        private const int SCALE_1_I3 = 6;
        private const int SCALE_1_I4 = 7;

        private const int SCALE_2_I1 = 8;
        private const int SCALE_2_I2 = 9;
        private const int SCALE_2_I3 = 10;
        private const int SCALE_2_I4 = 11;

        public SensorControl(
            Sensor sensor
            )
        {
            _sensor = sensor;
        }

        public bool IsInValidSensorScale1()
        {
            var connectStatus = _sensor.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return false;
            }

            var checkInScale1 = _sensor.ReadInputPort(SCALE_1_I1);
            var checkOutScale1 = _sensor.ReadInputPort(SCALE_1_I2);
            var checkLeftScale1 = _sensor.ReadInputPort(SCALE_1_I3);
            var checkRightScale1 = _sensor.ReadInputPort(SCALE_1_I4);

            if (checkInScale1 || checkOutScale1 || checkLeftScale1 || checkRightScale1)
            {
                return false;
            }

            return true;
        }

        public bool IsInValidSensorScale2()
        {
            var connectStatus = _sensor.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return false;
            }

            var checkInScale1 = _sensor.ReadInputPort(SCALE_2_I1);
            var checkOutScale1 = _sensor.ReadInputPort(SCALE_2_I2);
            var checkLeftScale1 = _sensor.ReadInputPort(SCALE_2_I3);
            var checkRightScale1 = _sensor.ReadInputPort(SCALE_2_I4);

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
