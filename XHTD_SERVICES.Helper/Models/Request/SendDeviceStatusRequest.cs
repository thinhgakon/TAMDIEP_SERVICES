using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendDeviceStatusRequest
    {
        public string DeviceCode { get; set; }

        public string Status { get; set; }
    }
}
