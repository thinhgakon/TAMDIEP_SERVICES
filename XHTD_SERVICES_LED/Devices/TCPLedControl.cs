using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Devices
{
    public class TCPLedControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly TCPLed _tcpLed;

        protected readonly string SCALE_1_LED_IN_CODE = ScaleLedCode.CODE_SCALE_1_LED_IN;

        protected readonly string SCALE_1_LED_OUT_CODE = ScaleLedCode.CODE_SCALE_1_LED_OUT;

        protected readonly string SCALE_2_LED_IN_CODE = ScaleLedCode.CODE_SCALE_2_LED_IN;

        protected readonly string SCALE_2_LED_OUT_CODE = ScaleLedCode.CODE_SCALE_2_LED_OUT;

        protected readonly string SCALE_1_LED_IN_URL = "192.168.22.41";

        protected readonly string SCALE_1_LED_OUT_URL = "192.168.22.43";

        protected readonly string SCALE_2_LED_IN_URL = "192.168.22.40";

        protected readonly string SCALE_2_LED_OUT_URL = "192.168.22.42";

        public TCPLedControl(TCPLed tcpLed)
        {
            _tcpLed = tcpLed;
        }

        public string GetIpAddress(string scaleLedCode)
        {
            var ipAddress = SCALE_1_LED_IN_URL;

            if (scaleLedCode == SCALE_1_LED_IN_CODE)
            {
                ipAddress = SCALE_1_LED_IN_URL;
            }
            else if (scaleLedCode == SCALE_1_LED_OUT_CODE)
            {
                ipAddress = SCALE_1_LED_OUT_URL;
            }
            else if (scaleLedCode == SCALE_2_LED_IN_CODE)
            {
                ipAddress = SCALE_2_LED_IN_URL;
            }
            else if (scaleLedCode == SCALE_2_LED_OUT_CODE)
            {
                ipAddress = SCALE_2_LED_OUT_URL;
            }

            return ipAddress;
        }

        public bool DisplayScreen(string scaleLedCode, string dataCode)
        {
            var ipAddress = GetIpAddress(scaleLedCode);

            //_logger.Info($"IP Led: {ipAddress}");

            _tcpLed.Connect(ipAddress);

            _tcpLed.SetDataCode(dataCode);

            return _tcpLed.DisplayScreen();
        }
    }
}
