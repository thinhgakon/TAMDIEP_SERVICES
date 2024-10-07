using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_CLK_TROUGH_1.Devices
{
    public class TCPLedControl
    {
        ILog _logger = LogManager.GetLogger("LedFileAppender");

        protected readonly TCPLed _tcpLed;

        public TCPLedControl(TCPLed tcpLed)
        {
            _tcpLed = tcpLed;
        }

        public bool DisplayScreen(string ipAddress, string dataCode)
        {
            _logger.Info($"DisplayScreen LED: IP={ipAddress} -- CODE: {dataCode}");

            _tcpLed.Connect(ipAddress);

            _tcpLed.SetDataCode(dataCode);

            return _tcpLed.DisplayScreen();
        }
    }
}
