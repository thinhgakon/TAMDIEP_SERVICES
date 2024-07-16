using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendConfirmNotificationRequest
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public string Vehicle { get; set; }
        public string CardNo { get; set; }
    }
}
