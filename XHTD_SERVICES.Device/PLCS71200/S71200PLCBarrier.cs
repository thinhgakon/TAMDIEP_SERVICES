using log4net;
using NDTan;
using S7.Net;
using System;

namespace XHTD_SERVICES.Device
{
    public class S71200PLCBarrier : S71200
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(S71200PLCBarrier));

        public S71200PLCBarrier(Plc plc) : base(plc)
        {
        }
    }
}
