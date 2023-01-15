using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDTan;

namespace XHTD_SERVICES.Device.PLCM221
{
    public class PLCBarrier : M221
    {
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

        public void ResetOutPort(int port)
        {
            if (ReadInputPort(port))
            {
                ShuttleOutputPort((byte.Parse(port.ToString())));
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

        public bool TurnOn(string ipAddress, int portNumber, int portNumberDeviceIn, int portNumberDeviceOut) {

            PLC_Result = Connect($"{ipAddress}", portNumber);

            if (PLC_Result == M221Result.SUCCESS)
            {
                bool[] Ports = new bool[24];
                PLC_Result = CheckInputPorts(Ports);

                if (PLC_Result == M221Result.SUCCESS)
                {
                    if (!Ports[portNumberDeviceIn])
                    {
                        PLC_Result = ShuttleOutputPort((byte.Parse(portNumberDeviceOut.ToString())));
                        if (PLC_Result != M221Result.SUCCESS)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Connect failed to PLC ... {GetLastErrorString()}");
                return false;
            }

            return true;
        }

        public bool TurnOff(string ipAddress, int portNumber, int portNumberDeviceIn, int portNumberDeviceOut) {

            PLC_Result = Connect($"{ipAddress}", portNumber);

            if (PLC_Result == M221Result.SUCCESS)
            {
                bool[] Ports = new bool[24];
                PLC_Result = CheckInputPorts(Ports);

                if (PLC_Result == M221Result.SUCCESS)
                {
                    if (Ports[portNumberDeviceIn])
                    {
                        PLC_Result = ShuttleOutputPort((byte.Parse(portNumberDeviceOut.ToString())));
                        if (PLC_Result != M221Result.SUCCESS)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Connect failed to PLC ... {GetLastErrorString()}");
                return false;
            }

            return true;
        }
    }
}
