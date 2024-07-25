using log4net;
using NDTan;
using S7.Net;
using System;

namespace XHTD_SERVICES.Device
{
    public class S71200Sensor : S71200
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(S71200Sensor));

        public S71200Sensor(Plc plc) : base(plc)
        {
        }
    }
}
