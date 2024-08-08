using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_XB_TROUGH_5.Models.Response
{
    public class HUBResponse
    {
        public string Type { get; set; }
        public string Source { get; set; }
        public int Status { get; set; }
        public string Content { get; set; }

        public HUBDataResponse Data { get; set; }
    }
}
