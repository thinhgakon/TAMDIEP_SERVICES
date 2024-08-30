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

        public readonly Dictionary<string, string> LedIpAddresses = new Dictionary<string, string>()
        {
            { MachineCode.MACHINE_XI_BAO_1, "192.168.13.190" },
            { MachineCode.MACHINE_XI_BAO_2, "192.168.13.195" },
            { MachineCode.MACHINE_XI_BAO_3, "192.168.13.211" },
            { MachineCode.MACHINE_XI_BAO_4, "192.168.13.216" },
            { MachineCode.MACHINE_MDB_1, "192.168.13.222" },
        };

        public TCPLedControl(TCPLed tcpLed)
        {
            _tcpLed = tcpLed;
        }

        public string GetIpAddress(string machineCode)
        {
            LedIpAddresses.TryGetValue(machineCode, out string ipAdress);
            return ipAdress;
        }

        public bool DisplayScreen(string machineCode, string dataCode)
        {
            var ipAddress = GetIpAddress(machineCode);

            _tcpLed.Connect(ipAddress);

            _tcpLed.SetDataCode(dataCode);

            return _tcpLed.DisplayScreen();
        }
    }
}
