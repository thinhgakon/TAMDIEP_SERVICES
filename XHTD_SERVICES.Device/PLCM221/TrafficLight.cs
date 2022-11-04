using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDTan;

namespace XHTD_SERVICES.Device.PLCM221
{
    public class TrafficLight : M221
    {
        public TrafficLight(PLC plc) : base(plc)
        {
        }
    }
}
