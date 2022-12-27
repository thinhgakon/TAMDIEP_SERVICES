using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM951_IN.Devices
{
    public class BarrierControl
    {
        protected readonly PLCBarrier _barrier;

        public BarrierControl(
            PLCBarrier barrier
            )
        {
            _barrier = barrier;
        }

        public bool OpenBarrier(string luong)
        {
            return _barrier.TurnOn("IpAddress", 1, 1, 1);
        }

        public bool CloseBarrier(string luong)
        {
            return _barrier.TurnOff("IpAddress", 1, 1, 1);
        }
    }
}
