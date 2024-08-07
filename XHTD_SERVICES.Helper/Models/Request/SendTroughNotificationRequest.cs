using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendTroughNotificationRequest
    {
        public string TroughType { get; set; }

        public string MachineCode { get; set; }

        public string TroughCode { get; set; }

        public string Vehicle { get; set; }
    }
}
