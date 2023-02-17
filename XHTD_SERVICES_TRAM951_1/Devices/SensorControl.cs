using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES_TRAM951_1.Hubs;
using XHTD_SERVICES.Data.Common;
using log4net;

namespace XHTD_SERVICES_TRAM951_1.Devices
{
    public class SensorControl
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SensorControl));

        protected readonly Sensor _sensor;

        private const string IP_ADDRESS = "10.0.9.6";

        private const int SCALE_I1 = 4;
        private const int SCALE_I2 = 5;
        private const int SCALE_I3 = 6;
        private const int SCALE_I4 = 7;

        protected readonly string SCALE_CB_1_CODE = ScaleCode.CODE_951_1_CB_1;

        protected readonly string SCALE_CB_2_CODE = ScaleCode.CODE_951_1_CB_2;

        protected readonly string SCALE_CB_3_CODE = ScaleCode.CODE_951_1_CB_3;

        protected readonly string SCALE_CB_4_CODE = ScaleCode.CODE_951_1_CB_4;

        public SensorControl(
            Sensor sensor
            )
        {
            _sensor = sensor;
        }

        public bool IsInValidSensorScale951()
        {
            var connectStatus = _sensor.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return false;
            }

            var checkCB1 = _sensor.ReadInputPort(SCALE_I1);
            var checkCB2 = _sensor.ReadInputPort(SCALE_I2);
            var checkCB3 = _sensor.ReadInputPort(SCALE_I3);
            var checkCB4 = _sensor.ReadInputPort(SCALE_I4);

            try
            {
                if (checkCB1)
                {
                    new ScaleHub().SendSensor(SCALE_CB_1_CODE, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(SCALE_CB_1_CODE, "0");
                }

                if (checkCB2)
                {
                    new ScaleHub().SendSensor(SCALE_CB_2_CODE, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(SCALE_CB_2_CODE, "0");
                }

                if (checkCB3)
                {
                    new ScaleHub().SendSensor(SCALE_CB_3_CODE, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(SCALE_CB_3_CODE, "0");
                }

                if (checkCB4)
                {
                    new ScaleHub().SendSensor(SCALE_CB_4_CODE, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(SCALE_CB_4_CODE, "0");
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Sensor Control ERROR: {ex.Message} ===== {ex.StackTrace} ==== {ex.InnerException}");
            }

            if (checkCB1 || checkCB3 || checkCB2 || checkCB4)
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
