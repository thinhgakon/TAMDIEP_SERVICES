using log4net;
using S7.Net;
using System.Threading;
using System;
using XHTD_SERVICES.Device.PLCS71200;

namespace XHTD_SERVICES_TRAM951_1.Devices
{
    public class S71200Control
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(S71200Control));

        protected readonly S71200PLCBarrier _barrier;

        private const string IP_ADDRESS = "192.168.13.166";
        private const short RACK = 0;
        private const short SLOT = 1;
        private const string GATEWAY_IN_Q1 = "Q0.0";
        private const string GATEWAY_OUT_Q1 = "Q0.1";

        public S71200Control()
        {
            var plc = new Plc(CpuType.S71200, IP_ADDRESS, RACK, SLOT);
            _barrier = new S71200PLCBarrier(plc);
        }

        public bool OpenBarrierIn()
        {
            var isConnectSuccessed = false;
            int count = 0;

            try
            {
                while (!isConnectSuccessed && count < 6)
                {
                    count++;
                    Console.WriteLine($@"OpenBarrierScaleIn: count={count}");
                    _logger.Info($@"OpenBarrierScaleIn: count={count}");

                    _barrier.Open();

                    if (_barrier.IsConnected)
                    {
                        _barrier.ShuttleOutputPort(GATEWAY_IN_Q1, true);

                        var batLan1 = _barrier.ReadOutputPort(GATEWAY_IN_Q1);

                        if (batLan1 == ErrorCode.NoError)
                        {
                            _logger.Info($"Bat lan 1 thanh cong");
                        }
                        else
                        {
                            _logger.Info($"Bat lan 1 that bai: {batLan1}");
                        }

                        Thread.Sleep(500);

                        _barrier.ShuttleOutputPort(GATEWAY_IN_Q1, false);

                        var batLan2 = _barrier.ReadOutputPort(GATEWAY_IN_Q1);

                        if (batLan2 == ErrorCode.NoError)
                        {
                            _logger.Info($"Bat lan 2 thanh cong");
                        }
                        else
                        {
                            _logger.Info($"Bat lan 2 that bai: {batLan2}");
                        }

                        Thread.Sleep(500);

                        _barrier.Close();

                        _logger.Info($"OpenBarrier count={count} thanh cong");

                        isConnectSuccessed = true;
                    }
                    else
                    {
                        _logger.Info($"OpenBarrier count={count}: Ket noi PLC khong thanh cong {ErrorCode.ConnectionError}");

                        Thread.Sleep(1000);
                    }
                }

                return isConnectSuccessed;
            }
            catch (Exception ex)
            {
                _logger.Info($"OpenBarrier Error: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
                return false;
            }
        }

        public bool OpenBarrierOut()
        {
            var isConnectSuccessed = false;
            int count = 0;

            try
            {
                while (!isConnectSuccessed && count < 6)
                {
                    count++;

                    _logger.Info($@"OpenBarrierScaleIn: count={count}");

                    _barrier.Open();

                    if (_barrier.IsConnected)
                    {
                        _barrier.ShuttleOutputPort(GATEWAY_OUT_Q1, true);

                        var batLan1 = _barrier.ReadOutputPort(GATEWAY_OUT_Q1);

                        if (batLan1 == ErrorCode.NoError)
                        {
                            _logger.Info($"Bat lan 1 thanh cong");
                        }
                        else
                        {
                            _logger.Info($"Bat lan 1 that bai: {batLan1}");
                        }

                        Thread.Sleep(500);

                        _barrier.ShuttleOutputPort(GATEWAY_OUT_Q1, false);

                        var batLan2 = _barrier.ReadOutputPort(GATEWAY_OUT_Q1);

                        if (batLan2 == ErrorCode.NoError)
                        {
                            _logger.Info($"Bat lan 2 thanh cong");
                        }
                        else
                        {
                            _logger.Info($"Bat lan 2 that bai: {batLan2}");
                        }

                        Thread.Sleep(500);

                        _barrier.Close();

                        _logger.Info($"OpenBarrier count={count} thanh cong");

                        isConnectSuccessed = true;
                    }
                    else
                    {
                        _logger.Info($"OpenBarrier count={count}: Ket noi PLC khong thanh cong {ErrorCode.ConnectionError}");

                        Thread.Sleep(1000);
                    }
                }

                return isConnectSuccessed;
            }
            catch (Exception ex)
            {
                _logger.Info($"OpenBarrier Error: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
                return false;
            }
        }

    }
}
