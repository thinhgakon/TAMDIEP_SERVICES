using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Data.Common
{
    public static class DeviceCode
    {
        public static Dictionary<string, List<(string, string)>> CodeDict = new Dictionary<string, List<(string, string)>>()
        {
            { LocationCode.CONFIRM,
                new List<(string, string)> {
                    ("DXT_UHF_2", "192.168.13.162"),
                    ("DXT_CAM", "192.168.13.163"),
                    ("DXT_DTH", "192.168.13.164")
                }
            },

            { LocationCode.GATEWAY,
                new List<(string, string)> {
                    ("CONG_PLC", "192.168.13.166"),
                    ("CONG_CAM_IN", "192.168.13.167"),
                    ("CONG_UHF_IN", "192.168.13.168"),
                    ("CONG_CAM_OUT", "192.168.13.169")
                }
            },

            { LocationCode.SCALE_IN,
                new List<(string, string)> {
                    ("TRAMCAN_PLC", "192.168.13.175"),
                    ("TRAMCAN_CAM_IN", "192.168.13.177"),
                    ("TRAMCAN_DTH_IN_1", "192.168.13.178"),
                    ("TRAMCAN_DTH_IN_2", "192.168.13.185"),
                    ("TRAMCAN_LED_IN", "192.168.13.180"),
                    ("TRAMCAN_UHF_IN_1", "192.168.13.181"),
                    ("TRAMCAN_UHF_IN_2", "192.168.13.182")
                }
            },

            { LocationCode.SCALE_OUT,
                new List<(string, string)> {
                    ("TRAMCAN_CAM_OUT", "192.168.13.183"),
                    ("TRAMCAN_DTH_OUT_1", "192.168.13.179"),
                    ("TRAMCAN_DTH_OUT_2", "192.168.13.184"),
                    ("TRAMCAN_LED_OUT", "192.168.13.186"),
                    ("TRAMCAN_UHF_OUT_1", "192.168.13.188"),
                    ("TRAMCAN_UHF_OUT_2", "192.168.13.187")
                }
            },

            { LocationCode.TROUGH_XI_BAO,
                new List<(string, string)> {
                    ("XIBAO_PLC_12", "192.168.13.189"),
                    ("XIBAO_LED_1", "192.168.13.190"),
                    ("XIBAO_UHF_1", "192.168.13.191"),
                    ("XIBAO_UHF_2", "192.168.13.193"),
                    ("XIBAO_LED_2", "192.168.13.195"),
                    ("XIBAO_UHF_3", "192.168.13.196"),
                    ("XIBAO_UHF_4", "192.168.13.198"),
                    ("XIBAO_PLC_34", "192.168.13.210"),
                    ("XIBAO_LED_3", "192.168.13.211"),
                    ("XIBAO_UHF_5", "192.168.13.212"),
                    ("XIBAO_UHF_6", "192.168.13.214"),
                    ("XIBAO_LED_4", "192.168.13.216"),
                    ("XIBAO_UHF_7", "192.168.13.217"),
                    ("XIBAO_UHF_8", "192.168.13.219"),
                    ("XIBAO_UHF_9", "192.168.13.242"),
                    ("XIBAO_UHF_10", "192.168.13.244"),
                    ("XIBAO_UHF_MDB_1", "192.168.13.222")
                }
            },

            { LocationCode.TROUGH_XI_ROI,
                new List<(string, string)>
                {
                    ("XIROI_UHF", "192.168.13.230"),
                    ("XIROI_LED", "192.168.13.232"),
                    ("XIROI_DTH", "192.168.13.233")
                }
            },

            { LocationCode.TROUGH_CLINKER,
                new List<(string, string)>
                {
                    ("CLK_UHF", "192.168.13.237"),
                    ("CLK_LED", "192.168.13.239")
                }
            }
        };
    }
}
