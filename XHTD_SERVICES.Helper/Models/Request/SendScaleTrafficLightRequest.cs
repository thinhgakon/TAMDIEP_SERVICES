using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendScaleTrafficLightRequest
    {
        public string TrafficLightCode { get; set; }

        public string Red { get; set; }

        public string Green { get; set; }
    }
}
