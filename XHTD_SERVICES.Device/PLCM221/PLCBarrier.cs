using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using NDTan;

namespace XHTD_SERVICES.Device.PLCM221
{
    public class PLCBarrier : M221
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PLCBarrier));

        private M221Result PLC_Result;

        public PLCBarrier(PLC plc) : base(plc)
        {
        }

        public M221Result ConnectPLC(string ipAddress)
        {
            return Connect($"{ipAddress}", 502);
        }

        public bool ReadInputPort(int portIn)
        {
            bool[] Ports = new bool[24];
            PLC_Result = CheckInputPorts(Ports);

            if (PLC_Result == M221Result.SUCCESS)
            {
                if (Ports[portIn])
                {
                   return true;
                }
            }
            else
            {
                return false;
            }

            return false;
        }

        public bool ReadOutputPort(int portOut)
        {
            bool[] Ports = new bool[15];
            PLC_Result = CheckOutputPorts(Ports);

            if (PLC_Result == M221Result.SUCCESS)
            {
                if (Ports[portOut])
                {
                    return true;
                }
            }
            else
            {
                return false;
            }

            return false;
        }

        public void ResetOutputPort(int portOut)
        {
            try { 
                if (ReadOutputPort(portOut))
                {
                    ShuttleOutputPort((byte.Parse(portOut.ToString())));
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void ResetAllOutputPorts()
        {
            bool[] Ports = new bool[15]; //There are 16 input ports on M221 PLC
            PLC_Result = CheckOutputPorts(Ports);
            if (PLC_Result == M221Result.SUCCESS)
            {
                for (int i = 0; i < Ports.Length; i++)
                {
                    if (Ports[i])
                    {
                        _logger.Info($"Reset output port: {i}");
                        Console.WriteLine($"Reset output port: {i}");

                        ShuttleOutputPort(byte.Parse(i.ToString()));
                    }
                }
            }
        }

        public bool TurnOnOutPort(int port)
        {
            if (!ReadInputPort(port))
            {
                var result = ShuttleOutputPort((byte.Parse(port.ToString())));
                if (result == M221Result.SUCCESS)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
