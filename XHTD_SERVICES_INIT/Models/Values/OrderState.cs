using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XHTD_SERVICES_INIT.Models.Values
{
    public enum  OrderState
    {
        [Display(Name = "Đã đặt hàng")]
        DA_DAT_HANG = 1,

        [Display(Name = "Đã hủy đơn")]
        DA_HUY_DON = 2,

        [Display(Name = "Đang lấy hàng")]
        DANG_LAY_HANG = 3,

        [Display(Name = "Đã xuất hàng")]
        DA_XUAT_HANG = 4,

    }
}
