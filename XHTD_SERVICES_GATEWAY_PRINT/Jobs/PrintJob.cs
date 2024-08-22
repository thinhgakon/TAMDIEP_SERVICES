using System;
using System.IO;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_INIT.Models.Values;
using XHTD_SERVICES.Helper;
using System.Diagnostics;
using System.Threading;

namespace XHTD_SERVICES_GATEWAY_PRINT.Jobs
{
    [DisallowConcurrentExecution]
    public class PrintJob : IJob
    {
        private readonly PrintRepository _printRepository;
        private readonly string PRINT_NAME = "Brother HL-L2360D series Printer";
        private readonly string PRINT_APP = "C:\\Program Files (x86)\\Foxit Software\\Foxit PDF Reader\\FoxitPDFReader.exe";
        private readonly PrintLogger _printLogger;
        public PrintJob(PrintRepository printRepository, PrintLogger printLogger)
        {
            _printRepository = printRepository;
            _printLogger = printLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await Print();
            });
        }

        private async Task Print()
        {
            var prints = await _printRepository.GetByStatus(PrintStatus.PENDING.ToString());

            if (prints == null || prints.Count == 0)
                return;

            _printLogger.LogInfo($"Tim thay lenh in: {prints.Count}");

            foreach (var print in prints)
            {
                var response = HttpRequest.PrintInvoice(print.ErpOrderId);

                string tempFilePath = Path.GetTempFileName() + ".pdf";

                File.WriteAllBytes(tempFilePath, response.RawBytes);
                Thread.Sleep(1000);
                _printLogger.LogInfo($"Get file thanh cong {tempFilePath}");
                try
                {
                    using (Process printProcess = new Process())
                    {
                        printProcess.StartInfo.FileName = PRINT_APP;
                        printProcess.StartInfo.Arguments = $"/t \"{tempFilePath}\" \"{PRINT_NAME}\"";
                        printProcess.StartInfo.CreateNoWindow = true;
                        printProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        printProcess.Start();
                    }

                    print.Status = PrintStatus.SUCCESS.ToString();
                    _printLogger.LogInfo($"In thanh cong {print.DeliveryCode}");

                    Thread.Sleep(5000);
                    File.Delete(tempFilePath);
                    _printLogger.LogInfo($"Xoa file tmp thanh cong");
                }
                catch (Exception ex)
                {
                    _printLogger.LogInfo($"{ex.Message}");
                    _printLogger.LogInfo($"{ex.StackTrace}");
                    continue;
                }
            }

            await _printRepository.UpdateRange(prints);
        }
    }
}
