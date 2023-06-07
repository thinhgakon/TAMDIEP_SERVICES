using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XHTD_SERVICES.Data.Models.Values
{
    public enum TransportMethod
    {
        [Display(Name = "Đường bộ")]
        DUONG_BO = 1,

        [Display(Name = "Đường thủy")]
        DUONG_THUY = 3,
    }
}
