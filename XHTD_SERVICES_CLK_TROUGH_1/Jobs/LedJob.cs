using Autofac;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES_CLK_TROUGH_1.Devices;

namespace XHTD_SERVICES_CLK_TROUGH_1.Jobs
{
    public class LedJob : IJob
    {
        ILog _logger = LogManager.GetLogger("LedFileAppender");

        protected readonly string LED_IP_ADDRESS = "192.168.13.239";

        private readonly string TROUGH_CODE = "15";

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

        public async void LEDProcess()
        {
            try
            {
                tblCallToTrough callToTrough = null;
                using (var db = new XHTD_Entities())
                {
                    callToTrough = await db.tblCallToTroughs
                                            .Where(x => x.Machine == TROUGH_CODE 
                                                        &&
                                                        (x.IsDone == null || x.IsDone == false)
                                                  )
                                            .OrderBy(x => x.IndexTrough)
                                            .FirstOrDefaultAsync();
                }

                string dataCode = $"*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG XUAT HANG KHONG DUNG[H3][C1]XIN MOI LAI XE[H4][C1]KIEM TRA VA XAC NHAN DON HANG[!]";

                if (callToTrough != null)
                {
                    dataCode = $"*[H1][C1]{callToTrough.Vehicle}[H2][C1][1]{callToTrough.DeliveryCode}[2]---[H3][C1][1]---[2]---[H4][C1][1]---[2]---[!]";
                }

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
