﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Repositories;
using System.Threading;

namespace XHTD_SERVICES_TRAM951_IN.Devices
{
    public class BarrierControl
    {
        protected readonly PLCBarrier _barrier;

        private const string IP_ADDRESS = "10.0.9.6";

        private const int SCALE_1_IN_I1 = 9;
        private const int SCALE_1_IN_Q1 = 8;
        private const int SCALE_1_IN_Q2 = 9;

        private const int SCALE_1_OUT_I1 = 11;
        private const int SCALE_1_OUT_Q1 = 10;
        private const int SCALE_1_OUT_Q2 = 11;

        private const int SCALE_2_IN_I1 = 13;
        private const int SCALE_2_IN_Q1 = 12;
        private const int SCALE_2_IN_Q2 = 13;

        private const int SCALE_2_OUT_I1 = 15;
        private const int SCALE_2_OUT_Q1 = 14;
        private const int SCALE_2_OUT_Q2 = 15;

        public BarrierControl(
            PLCBarrier barrier
            )
        {
            _barrier = barrier;
        }

        // Barrier chiều vào cân 1
        public void OpenBarrierScale1In1()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if(connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (_barrier.ReadInputPort(SCALE_1_IN_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_IN_Q1.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_IN_Q1.ToString())));
            }
        }

        public void CloseBarrierScaleIn1()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (!_barrier.ReadInputPort(SCALE_1_IN_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_IN_Q2.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_IN_Q2.ToString())));
            }
        }

        // Barrier chiều ra cân 1
        public void OpenBarrierScaleOut1()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (_barrier.ReadInputPort(SCALE_1_OUT_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_OUT_Q1.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_OUT_Q1.ToString())));
            }
        }

        public void CloseBarrierScaleOut1()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (!_barrier.ReadInputPort(SCALE_1_OUT_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_OUT_Q2.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_1_OUT_Q2.ToString())));
            }
        }

        // Barrier chiều vào cân 2
        public void OpenBarrierScaleIn2()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (_barrier.ReadInputPort(SCALE_2_IN_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_IN_Q1.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_IN_Q1.ToString())));
            }
        }

        public void CloseBarrierScaleIn2()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (!_barrier.ReadInputPort(SCALE_2_IN_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_IN_Q2.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_IN_Q2.ToString())));
            }
        }

        // Barrier chiều ra cân 2
        public void OpenBarrierScaleOut2()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (_barrier.ReadInputPort(SCALE_2_OUT_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_OUT_Q1.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_OUT_Q1.ToString())));
            }
        }

        public void CloseBarrierScaleOut2()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            if (!_barrier.ReadInputPort(SCALE_2_OUT_I1))
            {
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_OUT_Q2.ToString())));
                Thread.Sleep(1000);
                _barrier.ShuttleOutputPort((byte.Parse(SCALE_2_OUT_Q2.ToString())));
            }
        }
    }
}
