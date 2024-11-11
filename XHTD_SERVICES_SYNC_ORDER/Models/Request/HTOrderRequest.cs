using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_SYNC_ORDER.Models.Request
{
    public class HTOrderRequest
    {
        public string DELIVERY_CODE_TD { get; set; }
        public string DELIVERY_CODE_HT { get; set; }
        public string VEHICLE_CODE { get; set; }
        public string TIME_IN { get; set; }
        public string TIME_OUT { get; set; }
        public string ORDER_DATE { get; set; }
        public double LOADWEIGHTNULL { get; set; }
        public double LOADWEIGHTFULL { get; set; }
        public string SO_STATUS { get; set; }
        public double ORDER_QUANTITY { get; set; }
    }
}
