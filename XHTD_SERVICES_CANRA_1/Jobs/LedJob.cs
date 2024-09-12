using Autofac;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CANRA_1.Devices;

namespace XHTD_SERVICES_CANRA_1.Jobs
{
    public class LedJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        protected readonly string LED_IP_ADDRESS = "192.168.13.186";

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
                if (Program.scaleValuesForResetLight == null || Program.scaleValuesForResetLight.Count == 0)
                {
                    WriteLogInfo($"1. Không có chỉ số cân");

                    if (Program.IsFirstTimeResetLed)
                    {
                        WriteLogInfo($"2. Lần đầu tiên");

                        Program.IsFirstTimeResetLed = false;

                        string emptyDataCode = $"*[H1][C1]HE THONG CAN TU DONG[H2][C1]---[H3][C1]---[H4][Cy]---[!]";

                        WriteLogInfo($"3. Send Code: {emptyDataCode}");

                        DisplayScreenLed(emptyDataCode);
                    }
                    else
                    {
                        WriteLogInfo($"Không phải lần đầu tiên => Kết thúc");
                    }

                    return;
                }
                else
                {
                    Program.IsFirstTimeResetLed = true;
                }

                WriteLogInfo($"1. Có chỉ số cân");

                var weightText = Program.scaleValuesForResetLight.LastOrDefault();
                var vehicleText = Program.InProgressVehicleCode != null ? Program.InProgressVehicleCode.ToUpper() : "HE THONG CAN TU DONG";

                string dataCode = $"*[H1][C1]{vehicleText}[H2][C1]{weightText}[H3][C1]---[H4][Cy]---[!]";

                WriteLogInfo($"3. Send Code: {dataCode}");

                DisplayScreenLed(dataCode);
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
                WriteLogInfo("3.1. Thành công");
            }
            else
            {
                WriteLogInfo($"3.1. Thất bại");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
