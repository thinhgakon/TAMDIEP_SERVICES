using System;
using System.IO;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_INIT.Models.Values;
using XHTD_SERVICES.Helper;
using System.Diagnostics;

namespace XHTD_SERVICES_GATEWAY_PRINT.Jobs
{
    [DisallowConcurrentExecution]
    public class PrintJob : IJob
    {
        private readonly PrintRepository _printRepository;
        private readonly string PRINT_NAME = "Brother HL-L2360D series Printer";
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
                _printLogger.LogInfo($"Get file thanh cong {print.DeliveryCode}");
                try
                {
                    string powershellCommand = $"Get-Content \"{tempFilePath}\" | Out-Printer -Name \"{PRINT_NAME}\"";

                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"{powershellCommand}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process printProcess = new Process
                    {
                        StartInfo = processInfo
                    };
                    printProcess.Start();
                    printProcess.WaitForExit();
                    print.Status = PrintStatus.SUCCESS.ToString();
                    _printLogger.LogInfo($"In thanh cong {print.DeliveryCode}");
                }
                catch (Exception)
                {
                    continue;
                }
                finally
                {
                    File.Delete(tempFilePath);
                    _printLogger.LogInfo($"Xoa file tmp thanh cong");
                }
            }

            await _printRepository.UpdateRange(prints);
        }
    }
}
