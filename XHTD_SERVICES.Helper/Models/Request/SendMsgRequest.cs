using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendMsgRequest
    {
        public string Type { get; set; }
        public string Source { get; set; }
        public int Status { get; set; }
        public SendDataMsgRequest Data { get; set; }
    }
}
