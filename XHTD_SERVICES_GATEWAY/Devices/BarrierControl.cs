﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Repositories;
using System.Threading;
using log4net;

namespace XHTD_SERVICES_GATEWAY.Devices
{
    public class BarrierControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(BarrierControl));

        protected readonly PLCBarrier _barrier;

        private const string IP_ADDRESS = "10.0.9.2";

        private const int GATEWAY_IN_Q1 = 0;
        private const int GATEWAY_OUT_Q1 = 1;

        public BarrierControl(
            PLCBarrier barrier
            )
        {
            _barrier = barrier;
        }

        // Barrier chiều vào
        public bool OpenBarrierScaleIn()
        {
            try
            {
                M221Result isConnected = _barrier.ConnectPLC(IP_ADDRESS);

                if (isConnected == M221Result.SUCCESS)
                {
                    Thread.Sleep(500);

                    _barrier.ShuttleOutputPort(byte.Parse(GATEWAY_IN_Q1.ToString()));
                    Thread.Sleep(500);
                    _barrier.ShuttleOutputPort(byte.Parse(GATEWAY_IN_Q1.ToString()));

                    Thread.Sleep(500);

                    _barrier.Close();

                    _logger.Info("OpenBarrier thanh cong");

                    return true;
                }
                else
                {
                    _logger.Info("OpenBarrier: Ket noi PLC khong thanh cong");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"OpenBarrier Error: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
                return false;
            }
        }

        // Barrier chiều ra
        public bool OpenBarrierScaleOut()
        {
            try
            {
                M221Result isConnected = _barrier.ConnectPLC(IP_ADDRESS);

                if (isConnected == M221Result.SUCCESS)
                {
                    Thread.Sleep(500);

                    _barrier.ShuttleOutputPort(byte.Parse(GATEWAY_OUT_Q1.ToString()));
                    Thread.Sleep(500);
                    _barrier.ShuttleOutputPort(byte.Parse(GATEWAY_OUT_Q1.ToString()));

                    Thread.Sleep(500);

                    _barrier.Close();

                    _logger.Info("OpenBarrier thanh cong");

                    return true;
                }
                else
                {
                    _logger.Info("OpenBarrier: Ket noi PLC khong thanh cong");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"OpenBarrier Error: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
                return false;
            }
        }
    }
}
