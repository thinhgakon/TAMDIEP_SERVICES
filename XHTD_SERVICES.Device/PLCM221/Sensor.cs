using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDTan;

namespace XHTD_SERVICES.Device.PLCM221
{
    public class Sensor : M221
    {
        private M221Result PLC_Result;

        public Sensor(PLC plc) : base(plc)
        {
        }

        public bool CheckValid(string ipAddress, int portNumber, List<int> portNumberDeviceIns)
        {

            PLC_Result = Connect($"{ipAddress}", portNumber);

            if (PLC_Result == M221Result.SUCCESS)
            {
                bool[] Ports = new bool[24];
                PLC_Result = CheckInputPorts(Ports);

                if (PLC_Result == M221Result.SUCCESS)
                {
                    foreach(var portNumberDeviceIn in portNumberDeviceIns)
                    {
                        if (Ports[portNumberDeviceIn])
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
