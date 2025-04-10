﻿using log4net;
using Quartz;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Device.PLCS71200;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_CANRA_1.Hubs;

namespace XHTD_SERVICES_CANRA_1.Jobs
{
    public class SensorJob : IJob
    {
        private static readonly ILog _logger = LogManager.GetLogger("SensorFileAppender");

        protected readonly S71200Sensor _sensor;
        private readonly Notification _notification;

        private const string IP_ADDRESS = "192.168.13.175";
        private const short RACK = 0;
        private const short SLOT = 1;

        private const string SCALE_IN_I = "I0.2"; /*"Q0.2";*/
        private const string SCALE_OUT_I = "I0.3"; /*"Q0.2";*/

        protected readonly string SCALE_CB_1_CODE = "SCALE-2-CB-1";
        protected readonly string SCALE_CB_2_CODE = "SCALE-2-CB-3";

        public SensorJob(Notification notification)
        {
            var plc = new Plc(CpuType.S71200, IP_ADDRESS, RACK, SLOT);
            _sensor = new S71200Sensor(plc);
            _notification = notification;
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

                var checkScale2CB1 = _sensor.ReadInputPort(SCALE_IN_I);
                var checkScale2CB2 = _sensor.ReadInputPort(SCALE_OUT_I);

                if (checkScale2CB1) 
                {
                    _notification.SendScale2Sensor(SCALE_CB_1_CODE, "1");

                    WriteLogInfo($"Vi phạm cảm biến 1");
                }
                else
                {
                    _notification.SendScale2Sensor(SCALE_CB_1_CODE, "0");
                }

                Thread.Sleep(200);

                if (checkScale2CB2) 
                {
                    WriteLogInfo($"Vi phạm cảm biến 2");

                    _notification.SendScale2Sensor(SCALE_CB_2_CODE, "1");
                }
                else
                {
                    _notification.SendScale2Sensor(SCALE_CB_2_CODE, "0");
                }
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
