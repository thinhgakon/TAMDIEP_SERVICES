using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Data.Models.Response
{
    public class SearchOrderResponse
    {
        public double totalBookQuantity { get; set; }
        public double totalOrderQuantity { get; set; }
        public int total { get; set; }
        public List<OrderItemResponse> collection { get; set; }
    }
}
