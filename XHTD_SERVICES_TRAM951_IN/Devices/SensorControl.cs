using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES_TRAM951_IN.Hubs;
using XHTD_SERVICES.Data.Common;
using log4net;

namespace XHTD_SERVICES_TRAM951_IN.Devices
{
    public class SensorControl
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SensorControl));

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
            var checkLeftScale1 = _sensor.ReadInputPort(SCALE_1_I2);
            var checkOutScale1 = _sensor.ReadInputPort(SCALE_1_I3);
            var checkRightScale1 = _sensor.ReadInputPort(SCALE_1_I4);

            try { 
                if (checkInScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_1, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_1, "0");
                }

                if (checkLeftScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_2, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_2, "0");
                }

                if (checkOutScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_3, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_3, "0");
                }

                if (checkRightScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_4, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_1_CB_4, "0");
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Sensor Control ERROR: {ex.Message} ===== {ex.StackTrace} ==== {ex.InnerException}");
            }

            if (checkInScale1 || checkOutScale1 || checkLeftScale1 || checkRightScale1)
            {
                return true;
            }

            return false;
        }

        public bool IsInValidSensorScale2()
        {
            var connectStatus = _sensor.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return false;
            }

            var checkInScale1 = _sensor.ReadInputPort(SCALE_2_I1);
            var checkLeftScale1 = _sensor.ReadInputPort(SCALE_2_I2);
            var checkOutScale1 = _sensor.ReadInputPort(SCALE_2_I3);
            var checkRightScale1 = _sensor.ReadInputPort(SCALE_2_I4);

            try
            {
                if (checkInScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_1, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_1, "0");
                }

                if (checkLeftScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_2, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_2, "0");
                }

                if (checkOutScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_3, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_3, "0");
                }

                if (checkRightScale1)
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_4, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(ScaleCode.CODE_951_2_CB_4, "0");
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Sensor Control ERROR: {ex.Message} ===== {ex.StackTrace} ==== {ex.InnerException}");
            }

            if (checkInScale1 || checkOutScale1 || checkLeftScale1 || checkRightScale1)
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
