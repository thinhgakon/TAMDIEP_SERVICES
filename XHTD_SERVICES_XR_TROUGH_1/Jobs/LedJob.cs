using Autofac;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES_XR_TROUGH_1.Devices;

namespace XHTD_SERVICES_XR_TROUGH_1.Jobs
{
    public class LedJob : IJob
    {
        ILog _logger = LogManager.GetLogger("LedFileAppender");

        protected readonly string LED_IP_ADDRESS = "192.168.13.232";

        private readonly string TROUGH_CODE = "11";

        private readonly string DEFAULT_LED_CODE = $"*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG XUAT HANG KHONG DUNG[H3][C1]XIN MOI LAI XE[H4][C1]KIEM TRA VA XAC NHAN DON HANG[!]";

        public LedJob()
        {
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
                    WriteLogInfo($"=================== Start JOB - IP: {LED_IP_ADDRESS} ===================");

                    LEDProcess();
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public void LEDProcess()
        {
            try
            {
                string dataCode = DEFAULT_LED_CODE;

                if (Program.CurrentOrder != null)
                {
                    dataCode = $"*[H1][C1]{Program.CurrentOrder.ItemAlias}[H2][C1][1]BSX[2]{Program.CurrentOrder.Vehicle}[H3][C1][1]MSGH[2]{Program.CurrentOrder.DeliveryCode}[H4][C1][1]DAT[2]{Program.CurrentOrder.SumNumber}[!]";
                }

                if (Program.CurrentOrder.DeliveryCode != Program.PreviousOrder.DeliveryCode)
                {
                    DisplayScreenLed(dataCode);

                    if (dataCode != DEFAULT_LED_CODE)
                    {
                        TurnOnRedTrafficLight();
                    }
                    else
                    {
                        TurnOnGreenTrafficLight();
                    }

                    Program.PreviousOrder = Program.CurrentOrder;
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"ERROR: {ex.Message}");
            }
        }

        public void DisplayScreenLed(string dataCode)
        {
            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(LED_IP_ADDRESS, dataCode))
            {
                WriteLogInfo($"3.1. Gửi mã LED thành công - dataCode: {dataCode}");
            }
            else
            {
                WriteLogInfo($"3.1. Gửi mã LED thất bại - dataCode: {dataCode}");
            }
        }

        public void TurnOnRedTrafficLight()
        {
            WriteLogInfo($@"6.1. Bật đèn ĐỎ");
            if (DIBootstrapper.Init().Resolve<TCPTrafficLightControl>().TurnOnRedTrafficLight())
            {
                WriteLogInfo($@"6.1.1. Bật đèn ĐỎ thành công");
            }
            else
            {
                WriteLogInfo($@"6.1.1. Bật đèn ĐỎ thất bại");
            }
        }

        public void TurnOnGreenTrafficLight()
        {
            WriteLogInfo($@"6.1. Bật đèn XANH");
            if (DIBootstrapper.Init().Resolve<TCPTrafficLightControl>().TurnOnGreenTrafficLight())
            {
                WriteLogInfo($@"6.1.1. Bật đèn XANH thành công");
            }
            else
            {
                WriteLogInfo($@"6.1.1. Bật đèn XANH thất bại");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
