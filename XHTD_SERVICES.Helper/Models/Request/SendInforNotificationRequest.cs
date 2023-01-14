using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendInforNotificationRequest
    {
        public string UserNameSender { get; set; }
        public string UserNameReceiver { get; set; }
        public string ContentMessage { get; set; }
    }
}
