using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES_TRAM481.Hubs;
using XHTD_SERVICES.Data.Common;
using log4net;

namespace XHTD_SERVICES_TRAM481.Devices
{
    public class SensorControl
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SensorControl));

        protected readonly Sensor _sensor;

        private const string IP_ADDRESS = "10.0.20.2";

        private const int SCALE_481_I1 = 2;
        private const int SCALE_481_I2 = 3;
        private const int SCALE_481_I3 = 4;
        private const int SCALE_481_I4 = 5;

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

            var checkInScale481 = _sensor.ReadInputPort(SCALE_481_I2);
            var checkOutScale481 = _sensor.ReadInputPort(SCALE_481_I3);
            var checkLeftScale481 = _sensor.ReadInputPort(SCALE_481_I1);
            var checkRightScale481 = _sensor.ReadInputPort(SCALE_481_I4);

            try
            {
                if (checkInScale481)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_1, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_1, "0");
                }

                if (checkLeftScale481)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_2, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_2, "0");
                }

                if (checkOutScale481)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_3, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_3, "0");
                }

                if (checkRightScale481)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_4, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_481_CB_4, "0");
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Sensor Control ERROR: {ex.Message} ===== {ex.StackTrace} ==== {ex.InnerException}");
            }

            if (checkInScale481 || checkOutScale481 || checkLeftScale481 || checkRightScale481)
            {
                return true;
            }

            return false;
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
