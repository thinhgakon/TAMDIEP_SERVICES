using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendMachineNotificationRequest
    {
        public string MachineType { get; set; }

        public string MachineCode { get; set; }

        public string StartStatus { get; set; }

        public string StopStatus { get; set; }
    }
}
