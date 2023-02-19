using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Repositories;
using System.Threading;

namespace XHTD_SERVICES_TRAM951_1.Devices
{
    public class BarrierControl
    {
        protected readonly PLCBarrier _barrier;

        private const string IP_ADDRESS = "10.0.9.6";

        private const int SCALE_IN_I1 = 0;
        private const int SCALE_IN_Q1 = 0;
        private const int SCALE_IN_Q2 = 1;

        private const int SCALE_OUT_I1 = 1;
        private const int SCALE_OUT_Q1 = 2;
        private const int SCALE_OUT_Q2 = 3;

        public BarrierControl(
            PLCBarrier barrier
            )
        {
            _barrier = barrier;
        }

        // Barrier chiều vào
        public void OpenBarrierScaleIn()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (_barrier.ReadInputPort(SCALE_IN_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_IN_Q1.ToString())));
                Thread.Sleep(500);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_IN_Q1.ToString())));
            }
        }

        public void CloseBarrierScaleIn()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (!_barrier.ReadInputPort(SCALE_IN_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_IN_Q2.ToString())));
                Thread.Sleep(500);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_IN_Q2.ToString())));
            }
        }

        // Barrier chiều ra
        public void OpenBarrierScaleOut()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (_barrier.ReadInputPort(SCALE_OUT_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_OUT_Q1.ToString())));
                Thread.Sleep(500);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_OUT_Q1.ToString())));
            }
        }

        public void CloseBarrierScaleOut()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (!_barrier.ReadInputPort(SCALE_OUT_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_OUT_Q2.ToString())));
                Thread.Sleep(500);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_OUT_Q2.ToString())));
            }
        }
    }
}
