using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Data.Common
{
    public static class DeviceCode
    {
        public static Dictionary<string, string> CONFIRM = new Dictionary<string, string>()
        {
            { "DXT_UHF_2", "192.168.13.162" },
            { "DXT_CAM", "192.168.13.163" },
            { "DXT_DTH", "192.168.13.164" }
        };

        public static Dictionary<string, string> GATEWAY = new Dictionary<string, string>()
        {
            { "CONG_PLC", "192.168.13.166" },
            { "CONG_CAM_IN", "192.168.13.167" },
            { "CONG_UHF_IN", "192.168.13.168" }
        };

        public static Dictionary<string, string> SCALE_IN = new Dictionary<string, string>()
        {
            { "TRAMCAN_PLC", "192.168.13.175" },
            { "TRAMCAN_CAM_IN", "192.168.13.177" },
            { "TRAMCAN_DTH_IN_1", "192.168.13.178" },
            { "TRAMCAN_DTH_IN_2", "192.168.13.185" },
            { "TRAMCAN_LED_IN", "192.168.13.180" },
            { "TRAMCAN_UHF_IN_1", "192.168.13.181" },
            { "TRAMCAN_UHF_IN_2", "192.168.13.182" }
        };

        public static Dictionary<string, string> SCALE_OUT = new Dictionary<string, string>()
        {
            { "TRAMCAN_CAM_OUT", "192.168.13.183" },
            { "TRAMCAN_DTH_OUT_1", "192.168.13.179" },
            { "TRAMCAN_DTH_OUT_2", "192.168.13.184" },
            { "TRAMCAN_LED_OUT", "192.168.13.186" },
            { "TRAMCAN_UHF_OUT_1", "192.168.13.188" },
            { "TRAMCAN_UHF_OUT_2", "192.168.13.187" }
        };
    }
}
