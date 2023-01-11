using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Data.Common
{
    public static class URIConfig
    {
        public static readonly string SIGNALR_START_ON_TRAM951_IN_SERVICE_URL = "http://10.0.1.41:8083";

        public static readonly string SIGNALR_START_ON_TRAM951_OUT_SERVICE_URL = "http://10.0.1.41:8084";

        public static readonly string SIGNALR_START_ON_TRAM481_SERVICE_URL = "http://10.0.1.41:8085";

        public static readonly string SIGNALR_GATEWAY_SERVICE_URL = "http://10.0.1.41:8083/signalr";

        //public static readonly string SIGNALR_START_ON_TRAM951_IN_SERVICE_URL = "http://localhost:8083";

        //public static readonly string SIGNALR_START_ON_TRAM951_OUT_SERVICE_URL = "http://localhost:8084";

        //public static readonly string SIGNALR_START_ON_TRAM481_SERVICE_URL = "http://localhost:8085";

        //public static readonly string SIGNALR_GATEWAY_SERVICE_URL = "http://localhost:8083/signalr";
    }
}
