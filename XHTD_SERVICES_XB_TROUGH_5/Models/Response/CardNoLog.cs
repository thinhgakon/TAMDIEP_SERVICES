using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_XB_TROUGH_5.Models.Response
{
    public class CardNoLog
    {
        public string CardNo { get; set; }
        public DateTime? DateTime { get; set; }
        public string LogCat => String.Format($@"{CardNo}==={DateTime}");
    }
}
