using log4net;
using Quartz;
using S7.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCS71200;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_CANVAO_1.Hubs;

namespace XHTD_SERVICES_CANVAO_1.Jobs
{
    public class SensorJob : IJob
    {
        private static readonly ILog _logger = LogManager.GetLogger("SensorFileAppender");

        protected readonly S71200Sensor _sensor;
        private readonly Notification _notification;

        private const string IP_ADDRESS = "192.168.13.175";
        private const short RACK = 0;
        private const short SLOT = 1;

        private const string SCALE_1_IN_I = "I0.1"; /*"Q0.0"; */
        private const string SCALE_1_OUT_I = "I0.0"; /*"Q0.2";*/

        protected readonly string SCALE_1_CB_1_CODE = "SCALE-1-CB-1";
        protected readonly string SCALE_1_CB_2_CODE = "SCALE-1-CB-3";

        public SensorJob(
            )
        {
            var plc = new Plc(CpuType.S71200, IP_ADDRESS, RACK, SLOT);
            _sensor = new S71200Sensor(plc);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                await Task.Run(() =>
                {
                    WriteLogInfo($"=================== Start JOB - IP: {IP_ADDRESS} ===================");

                    SendSensorJob();
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public void SendSensorJob()
        {
            try
            {
                _sensor.Open();

                if (!_sensor.IsConnected) return;

                var checkScale1CB1 = _sensor.ReadInputPort(SCALE_1_IN_I);
                var checkScale1CB2 = _sensor.ReadInputPort(SCALE_1_OUT_I);

                _notification.SendScale2Sensor(SCALE_1_CB_1_CODE, checkScale1CB1 ? "1" : "0");

                Thread.Sleep(200);

                _notification.SendScale2Sensor(SCALE_1_CB_2_CODE, checkScale1CB2 ? "1" : "0");
            }
            catch (Exception ex)
            {
                WriteLogInfo($"ERROR: {ex.Message}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
