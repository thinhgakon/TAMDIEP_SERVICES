using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_CANRA_2.Devices
{
    public class TCPLedControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly TCPLed _tcpLed;

        public TCPLedControl(TCPLed tcpLed)
        {
            _tcpLed = tcpLed;
        }

        public bool DisplayScreen(string ipAddress, string dataCode)
        {
            _tcpLed.Connect(ipAddress);

            _tcpLed.SetDataCode(dataCode);

            return _tcpLed.DisplayScreen();
        }
    }
}
