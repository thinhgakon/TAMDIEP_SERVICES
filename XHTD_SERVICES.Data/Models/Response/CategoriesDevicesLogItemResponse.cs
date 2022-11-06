using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Data.Models.Response
{
    public class CategoriesDevicesLogItemResponse
    {
        public string Code { get; set; }
        public int? ActionType { get; set; }
        public string ActionInfo { get; set; }
        public DateTime? ActionDate { get; set; }
    }
}
