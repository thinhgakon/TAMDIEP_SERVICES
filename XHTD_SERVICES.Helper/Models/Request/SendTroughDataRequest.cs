using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendTroughDataRequest
    {
        public string TroughType { get; set; }

        public string DeliveryCode { get; set; }

        public string MachineCode { get; set; }

        public string TroughCode { get; set; }

        public int? FirstQuantity { get; set; }

        public int? LastQuantity { get; set; }
    }
}
