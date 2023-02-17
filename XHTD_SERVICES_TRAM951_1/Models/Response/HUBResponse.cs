using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_TRAM951_1.Models.Response
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
