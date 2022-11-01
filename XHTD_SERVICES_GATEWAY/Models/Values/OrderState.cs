using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XHTD_SERVICES_GATEWAY.Models.Values
{
    public enum  OrderState
    {
        [Display(Name = "Đã xác nhận")]
        DA_XAC_NHAN = 1,

        [Display(Name = "Đã hủy")]
        DA_HUY = 2,

        [Display(Name = "Đã in phiếu")]
        DA_IN_PHIEU = 4,

        [Display(Name = "Đang lấy hàng")]
        DANG_LAY_HANG = 5,

        [Display(Name = "Đã xuất hàng")]
        DA_XUAT_HANG = 6,

    }
}
