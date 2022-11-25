using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendDataMsgRequest
    {
        public int Orderid { get; set; }
        public string DeliveryCode { get; set; }
        public int Rfid { get; set; }
        public string Vehicle { get; set; }
        public string DriverName { get; set; }
        public string DriverUserName { get; set; }
    }
}
