using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendTroughRfidRequest
    {
        public string LocationCode { get; set; }

        public string Rfid { get; set; }
    }
}
