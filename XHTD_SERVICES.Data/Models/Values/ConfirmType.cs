using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XHTD_SERVICES.Data.Models.Values
{
    public enum ConfirmType
    {
        [Display(Name = "Thủ công")]
        MANUAL = 0,

        [Display(Name = "Rfid")]
        RFID = 1,
    }
}
