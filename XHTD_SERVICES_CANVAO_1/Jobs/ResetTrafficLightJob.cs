using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device;
using XHTD_SERVICES_CANVAO_1.Devices;
using XHTD_SERVICES_CANVAO_1.Business;
using log4net;

namespace XHTD_SERVICES_CANVAO_1.Jobs
{
    [DisallowConcurrentExecution]
    public class ResetTrafficLightJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ResetLightFileAppender");

        protected readonly TCPTrafficLight _trafficLight;

        private tblCategoriesDevice trafficLight;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_1_DGT_IN;
        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_1_DGT_OUT;

        protected readonly string SCALE_CODE = ScaleCode.CODE_SCALE_1;

        protected readonly bool IsWillResetLight = true;

        public ResetTrafficLightJob(TCPTrafficLight trafficLight)
        {
            _trafficLight = trafficLight;
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
                    WriteLogInfo("--------------- START JOB ---------------");

                    TrafficLightProcess();
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public void TrafficLightProcess()
        {
            try
            {
                if (Program.scaleValuesForResetLight == null || Program.scaleValuesForResetLight.Count == 0)
                {
                    WriteLogInfo("1. Không có xe trên cân");

                    if (Program.IsFirstTimeResetTrafficLight)
                    {
                        Program.IsFirstTimeResetTrafficLight = false;

                        if (IsWillResetLight)
                        {
                            WriteLogInfo("2. Lần đầu tiên chỉ số cân về 0 => Tắt đèn");
                            TurnOffTrafficLight();
                        }
                        else
                        {
                            WriteLogInfo("2. Lần đầu tiên chỉ số cân về 0 => Job không tat đèn => Ket thuc");
                        }
                    }
                    else
                    {
                        WriteLogInfo("2. Không phải lần đầu tiên chỉ số cân về 0 => Kết thúc");
                    }

                    return;
                }
                else
                {
                    WriteLogInfo($"1. Có khối lượng cân: {Program.scaleValuesForResetLight.FirstOrDefault()} => Kết thúc");

                    Program.IsFirstTimeResetTrafficLight = true;
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RESET DGT ERROR: {ex.Message}");
            }
        }

        public void TurnOffTrafficLight()
        {
            WriteLogInfo($@"2.1. Tắt đèn chiều vào");
            try
            {
                if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOffTrafficLight(SCALE_DGT_IN_CODE))
                {
                    WriteLogInfo($@"2.1.1. Tắt thành công");
                }
                else
                {
                    WriteLogInfo($@"2.1.1. Tắt thất bại");
                }
            }
            catch(Exception ex) 
            {
                WriteLogInfo($@"2.1.1. ERROR: {ex.Message} -- {ex.StackTrace} -- {ex.InnerException}");
            }

            Thread.Sleep(500);

            WriteLogInfo($@"2.2. Tắt đèn chiều ra");
            try
            {
                if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOffTrafficLight(SCALE_DGT_OUT_CODE))
                {
                    WriteLogInfo($@"2.2.1. Tắt thành công");
                }
                else
                {
                    WriteLogInfo($@"2.2.1. Tắt thất bại");
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($@"2.1.1. ERROR: {ex.Message} -- {ex.StackTrace} -- {ex.InnerException}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
