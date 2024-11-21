using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device.PLCS71200;
using XHTD_SERVICES_CANVAO_2.Hubs;
using XHTD_SERVICES.Data.Common;
using log4net;
using S7.Net;
using System.Threading;

namespace XHTD_SERVICES_CANVAO_2.Devices
{
    public class S7SensorControl
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SensorControl));

        protected readonly S71200Sensor _sensor;

        private const string IP_ADDRESS = "192.168.13.175";
        private const short RACK = 0;
        private const short SLOT = 1;

        private const string SCALE_IN_I = "I0.1"; /*"Q0.0"; */
        private const string SCALE_OUT_I = "I0.0"; /*"Q0.2";*/

        protected readonly string SCALE_CB_1_CODE = ScaleCode.CODE_951_1_CB_1;

        protected readonly string SCALE_CB_2_CODE = ScaleCode.CODE_951_1_CB_2;

        public S7SensorControl(
            )
        {
            var plc = new Plc(CpuType.S71200, IP_ADDRESS, RACK, SLOT);
            _sensor = new S71200Sensor(plc);
        }

        public bool IsInValidSensorScale()
        {
            _sensor.Open();

            if (!_sensor.IsConnected)
            {
                return false;
            }

            var checkCB1 = _sensor.ReadInputPort(SCALE_IN_I);
            var checkCB2 = _sensor.ReadInputPort(SCALE_OUT_I);

            try
            {
                if (checkCB1)
                {
                    new ScaleHub().SendSensor(SCALE_CB_1_CODE, "1");
                    new ScaleHub().SendSensorAPI(SCALE_CB_1_CODE, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(SCALE_CB_1_CODE, "0");
                    new ScaleHub().SendSensorAPI(SCALE_CB_1_CODE, "0");
                }

                Thread.Sleep(200);

                if (checkCB2)
                {
                    new ScaleHub().SendSensor(SCALE_CB_2_CODE, "1");
                    new ScaleHub().SendSensorAPI(SCALE_CB_2_CODE, "1");
                }
                else
                {
                    new ScaleHub().SendSensor(SCALE_CB_2_CODE, "0");
                    new ScaleHub().SendSensorAPI(SCALE_CB_2_CODE, "0");
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Sensor Control ERROR: {ex.Message} ===== {ex.StackTrace} ==== {ex.InnerException}");
            }

            if (checkCB1 || checkCB2)
            {
                return true;
            }

            return false;
        }
    }
}
