using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDTan;

namespace XHTD_SERVICES.Device.PLCM221
{
    public class Barrier : M221
    {
        private M221Result PLC_Result;

        public Barrier(PLC plc) : base(plc)
        {
        }

        public bool TurnOn(string ipAddress, int portNumber, int portNumberDeviceIn, int portNumberDeviceOut) {

            PLC_Result = Connect($"{ipAddress}", portNumber);

            if (PLC_Result == M221Result.SUCCESS)
            {
                Console.WriteLine($"Connected to PLC ... {GetLastErrorString()}");

                bool[] Ports = new bool[24];
                PLC_Result = CheckInputPorts(Ports);

                if (PLC_Result == M221Result.SUCCESS)
                {
                    if (!Ports[portNumberDeviceIn])
                    {
                        PLC_Result = ShuttleOutputPort((byte.Parse(portNumberDeviceOut.ToString())));
                        if (PLC_Result == M221Result.SUCCESS)
                        {
                            Console.WriteLine("Open barrier: OK");
                        }
                        else
                        {
                            Console.WriteLine("Open barrier: ERROR");
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
                Console.WriteLine($"Connected to PLC ... {GetLastErrorString()}");

                bool[] Ports = new bool[24];
                PLC_Result = CheckInputPorts(Ports);

                if (PLC_Result == M221Result.SUCCESS)
                {
                    if (Ports[portNumberDeviceIn])
                    {
                        PLC_Result = ShuttleOutputPort((byte.Parse(portNumberDeviceOut.ToString())));
                        if (PLC_Result == M221Result.SUCCESS)
                        {
                            Console.WriteLine("Open barrier: OK");
                        }
                        else
                        {
                            Console.WriteLine("Open barrier: ERROR");
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
