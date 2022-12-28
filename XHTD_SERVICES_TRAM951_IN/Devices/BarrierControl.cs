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

        private const string IP_ADDRESS = "10.0.9.6";

        private const int SCALE_1_IN_Q1 = 1;
        private const int SCALE_1_IN_Q2 = 2;
        private const int SCALE_1_OUT_Q1 = 3;
        private const int SCALE_1_OUT_Q2 = 4;

        private const int SCALE_2_IN_Q1 = 5;
        private const int SCALE_2_IN_Q2 = 6;
        private const int SCALE_2_OUT_Q1 = 7;
        private const int SCALE_2_OUT_Q2 = 8;

        public BarrierControl(
            PLCBarrier barrier
            )
        {
            _barrier = barrier;
        }

        public void OpenBarrierScale1()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if(connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            // barrier đầu cân vào
            _barrier.ResetOutPort(SCALE_1_IN_Q1);
            _barrier.ResetOutPort(SCALE_1_IN_Q2);

            _barrier.TurnOnOutPort(SCALE_1_IN_Q1);

            // barrier đầu cân ra
            _barrier.ResetOutPort(SCALE_1_OUT_Q1);
            _barrier.ResetOutPort(SCALE_1_OUT_Q2);

            _barrier.TurnOnOutPort(SCALE_1_OUT_Q1);
        }

        public void CloseBarrierScale1()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            // barrier đầu cân vào
            _barrier.ResetOutPort(SCALE_1_IN_Q1);
            _barrier.ResetOutPort(SCALE_1_IN_Q2);

            _barrier.TurnOnOutPort(SCALE_1_IN_Q2);

            // barrier đầu cân ra
            _barrier.ResetOutPort(SCALE_1_OUT_Q1);
            _barrier.ResetOutPort(SCALE_1_OUT_Q2);

            _barrier.TurnOnOutPort(SCALE_1_OUT_Q2);
        }

        public void OpenBarrierScale2()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            // barrier đầu cân vào
            _barrier.ResetOutPort(SCALE_2_IN_Q1);
            _barrier.ResetOutPort(SCALE_2_IN_Q2);

            _barrier.TurnOnOutPort(SCALE_2_IN_Q1);

            // barrier đầu cân ra
            _barrier.ResetOutPort(SCALE_2_OUT_Q1);
            _barrier.ResetOutPort(SCALE_2_OUT_Q2);

            _barrier.TurnOnOutPort(SCALE_2_OUT_Q1);
        }

        public void CloseBarrierScale2()
        {
            var connectStatus = _barrier.ConnectPLC(IP_ADDRESS);

            if (connectStatus != M221Result.SUCCESS)
            {
                return;
            }

            // barrier đầu cân vào
            _barrier.ResetOutPort(SCALE_2_IN_Q1);
            _barrier.ResetOutPort(SCALE_2_IN_Q2);

            _barrier.TurnOnOutPort(SCALE_2_IN_Q2);

            // barrier đầu cân ra
            _barrier.ResetOutPort(SCALE_2_OUT_Q1);
            _barrier.ResetOutPort(SCALE_2_OUT_Q2);

            _barrier.TurnOnOutPort(SCALE_2_OUT_Q2);
        }
    }
}
