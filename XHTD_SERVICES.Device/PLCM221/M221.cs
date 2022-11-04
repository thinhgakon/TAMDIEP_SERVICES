using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDTan;

namespace XHTD_SERVICES.Device.PLCM221
{
    public abstract class M221
    {
        protected readonly PLC _plc;
        public M221(PLC plc)
        {
            _plc = plc;
            _plc.Mode = Mode.TCP_IP;
            _plc.ResponseTimeout = 1000;
        }

        public Result Connect(string ipAddress, int port)
        {
            return _plc.Connect(ipAddress, port);
        }

        public Result CheckInputPorts(bool[] PortsValue)
        {
            return _plc.CheckInputPorts(PortsValue);
        }

        public Result ShuttleOutputPort(byte q)
        {
            return _plc.ShuttleOutputPort(q);
        }

        public string GetLastErrorString()
        {
            return _plc.GetLastErrorString();
        }

        public void Close()
        {
            _plc.Close();
        }
    }
}
