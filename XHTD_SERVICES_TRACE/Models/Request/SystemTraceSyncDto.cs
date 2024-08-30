using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_TRACE.Models.Request
{
    public class SystemTraceSyncDto
    {
        public string Code { get; set; }

        public bool Status { get; set; }

        public string Address { get; set; }
    }

    public class SystemTraceDto
    {
        public string Code { get; set; }

        public string MachineName { get; set; }

        public bool? Status { get; set; }

        public string Log { get; set; }
    }
    public class SystemTraceServiceInfoDto
    {
        public string Code { get; set; }

        public int Interval { get; set; }
    }

    public class SystemTraceStartStopDto
    {
        public string Code { get; set; }
        public string MachineName { get; set; }
    }
}
