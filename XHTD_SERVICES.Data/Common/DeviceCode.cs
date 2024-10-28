using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Data.Common
{
    public static class DeviceCode
    {
        public static Dictionary<string, List<(string, string, string)>> CodeDict = new Dictionary<string, List<(string, string, string)>>()
        {
            { LocationCode.CONFIRM,
                new List<(string, string, string)> {
                    ("DXT_UHF_2", "192.168.13.162", "Điểm xác thực"),
                    ("DXT_CAM", "192.168.13.163", "Điểm xác thực"),
                    ("DXT_DTH", "192.168.13.164", "Điểm xác thực")
                }
            },

            { LocationCode.GATEWAY,
                new List<(string, string, string)> {
                    ("CONG_PLC", "192.168.13.166", "Cổng ra vào"),
                    ("CONG_CAM_IN", "192.168.13.167", "Cổng ra vào"),
                    ("CONG_UHF_IN", "192.168.13.168", "Cổng ra vào"),
                    ("CONG_CAM_OUT", "192.168.13.169", "Cổng ra vào")
                }
            },

            { LocationCode.SCALE_IN,
                new List<(string, string, string)> {
                    ("TRAMCAN_PLC", "192.168.13.175", "Trạm cân vào"),
                    ("TRAMCAN_CAM_IN", "192.168.13.177", "Trạm cân vào"),
                    ("TRAMCAN_DTH_IN_1", "192.168.13.178", "Trạm cân vào"),
                    ("TRAMCAN_DTH_IN_2", "192.168.13.185", "Trạm cân vào"),
                    ("TRAMCAN_LED_IN", "192.168.13.180", "Trạm cân vào"),
                    ("TRAMCAN_UHF_IN_1", "192.168.13.181", "Trạm cân vào"),
                    ("TRAMCAN_UHF_IN_2", "192.168.13.182", "Trạm cân vào")
                }
            },

            { LocationCode.SCALE_OUT,
                new List<(string, string, string)> {
                    ("TRAMCAN_CAM_OUT", "192.168.13.183", "Trạm cân ra"),
                    ("TRAMCAN_DTH_OUT_1", "192.168.13.179", "Trạm cân ra"),
                    ("TRAMCAN_DTH_OUT_2", "192.168.13.184", "Trạm cân ra"),
                    ("TRAMCAN_LED_OUT", "192.168.13.186", "Trạm cân ra"),
                    ("TRAMCAN_UHF_OUT_1", "192.168.13.188", "Trạm cân ra"),
                    ("TRAMCAN_UHF_OUT_2", "192.168.13.187", "Trạm cân ra")
                }
            }
        };
    }
}
