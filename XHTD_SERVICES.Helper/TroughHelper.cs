using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper
{
    public static class TroughHelper
    {
        public static string GetSimilarTroughCode(string troughCode)
        {
            switch (troughCode)
            {
                case "1":
                    return "2";
                case "2":
                    return "1";
                case "3":
                    return "4";
                case "4":
                    return "3";
                case "5":
                    return "6";
                case "6":
                    return "5";
                case "7":
                    return "8";
                case "8":
                    return "7";
                default:
                    return "1";
            }
        }
    }
}
