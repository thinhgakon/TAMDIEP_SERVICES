using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XHTD_SERVICES.Data.Models.Values
{
    public enum OrderStep
    {
        [Display(Name = "Chưa xác thực")]
        CHUA_XAC_THUC = 0,

        [Display(Name = "Đã xác thực ")]
        DA_XAC_THUC = 1,

        [Display(Name = "Đã vào cổng")]
        DA_VAO_CONG = 2,

        [Display(Name = "Đã cân vào")]
        DA_CAN_VAO = 3,

        [Display(Name = "Đang gọi xe ")]
        DANG_GOI_XE = 4,

        [Display(Name = "Đang lấy hàng ")]
        DANG_LAY_HANG = 5,

        [Display(Name = "Đã lấy hàng")]
        DA_LAY_HANG = 6,

        [Display(Name = "Đã cân ra")]
        DA_CAN_RA = 7,

        [Display(Name = "Đã hoàn thành")]
        DA_HOAN_THANH = 8,
    }
}
