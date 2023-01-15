using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper
{
    public static class LedHelper
    {
        public static string GetDisplayStatus(int step)
        {
            switch (step)
            {
                case 3:
                    return "DANG CHO";
                case 4:
                    return "DANG GOI";
                case 5:
                    return "DANG XUAT";
                case 6:
                    return "DANG XUAT";
                default:
                    return "DANG CHO";
            }
        }
    }
}
