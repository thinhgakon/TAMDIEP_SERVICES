﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Common;
using log4net;

namespace XHTD_SERVICES_CANVAO_2.Devices
{
    public class TrafficLightControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly string SCALE_1_DGT_IN_CODE = ScaleCode.CODE_SCALE_1_DGT_IN;

        protected readonly string SCALE_1_DGT_OUT_CODE = ScaleCode.CODE_SCALE_1_DGT_OUT;

        protected readonly string SCALE_2_DGT_IN_CODE = ScaleCode.CODE_SCALE_2_DGT_IN;

        protected readonly string SCALE_2_DGT_OUT_CODE = ScaleCode.CODE_SCALE_2_DGT_OUT;

        protected readonly string SCALE_1_DGT_IN_URL = "192.168.13.178";

        protected readonly string SCALE_1_DGT_OUT_URL = "192.168.13.185";

        protected readonly string SCALE_2_DGT_IN_URL = "192.168.13.184";

        protected readonly string SCALE_2_DGT_OUT_URL = "192.168.13.179";

        public TrafficLightControl(
            TCPTrafficLight trafficLight
            )
        {
            _trafficLight = trafficLight;
        }

        public string GetIpAddress(string scaleCode)
        {
            var ipAddress = SCALE_1_DGT_IN_URL;

            if (scaleCode == SCALE_1_DGT_IN_CODE)
            {
                ipAddress = SCALE_1_DGT_IN_URL;
            }
            else if (scaleCode == SCALE_1_DGT_OUT_CODE)
            {
                ipAddress = SCALE_1_DGT_OUT_URL;
            }
            else if (scaleCode == SCALE_2_DGT_IN_CODE)
            {
                ipAddress = SCALE_2_DGT_IN_URL;
            }
            else if (scaleCode == SCALE_2_DGT_OUT_CODE)
            {
                ipAddress = SCALE_2_DGT_OUT_URL;
            }

            return ipAddress;
        }

        public bool TurnOnGreenTrafficLight(string scaleCode)
        {
            try
            {
                var ipAddress = GetIpAddress(scaleCode);

                _logger.Info($"IP đèn: {ipAddress}");

                _trafficLight.Connect(ipAddress);

                return _trafficLight.TurnOnGreenOffRed();
            }
            catch (Exception ex)
            {
                _logger.Error($"Có lỗi xảy ra khi bật đèn XANH - {ex.Message}");

                return false;
            }
        }

        public bool TurnOnRedTrafficLight(string scaleCode)
        {
            try
            {
                var ipAddress = GetIpAddress(scaleCode);

                _logger.Info($"IP đèn: {ipAddress}");

                _trafficLight.Connect(ipAddress);

                return _trafficLight.TurnOffGreenOnRed();
            }
            catch (Exception ex)
            {
                _logger.Error($"Có lỗi xảy ra khi bật đèn ĐỎ - {ex.Message}");

                return false;
            }
        }

        public bool TurnOffTrafficLight(string scaleCode)
        {
            try
            {
                var ipAddress = GetIpAddress(scaleCode);

                _logger.Info($"IP đèn: {ipAddress}");

                _trafficLight.Connect(ipAddress);

                return _trafficLight.TurnOffGreenOffRed();
            }
            catch (Exception ex)
            {
                _logger.Error($"Có lỗi xảy ra khi tắt đèn - {ex.Message}");

                return false;
            }
        }
    }
}
