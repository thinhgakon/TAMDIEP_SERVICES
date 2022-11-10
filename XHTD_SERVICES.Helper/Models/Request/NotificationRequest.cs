using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class NotificationRequest
    {
        public string FromService { get; set; }
        public string FromDevice { get; set; }
        public string Vehicle { get; set; }
        public string CardNo { get; set; }
        public string DeliveryCode { get; set; }
        public string Content { get; set; }
    }
}
