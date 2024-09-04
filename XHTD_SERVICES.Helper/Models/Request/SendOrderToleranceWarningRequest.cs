using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendOrderToleranceWarningRequest
    {
        public string DeliveryCode { get; set; }

        public string Vehicle { get; set; }

        public decimal? SumNumber { get; set; }

        public int? WeightIn { get; set; }

        public int? WeightOut { get; set; }

        public double? Tolerance { get; set; }
    }
}
