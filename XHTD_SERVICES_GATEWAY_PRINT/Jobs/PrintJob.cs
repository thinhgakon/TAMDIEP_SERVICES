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
        private readonly string PRINT_NAME = @"\\192.168.13.171\printname";

        public PrintJob(PrintRepository printRepository)
        {
            _printRepository = printRepository;
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

            foreach (var print in prints)
            {
                var response = HttpRequest.PrintInvoice(print.ErpOrderId);

                string tempFilePath = Path.GetTempFileName() + ".pdf";

                File.WriteAllBytes(tempFilePath, response.RawBytes);
                try
                {
                    string command = $"/c print /d:\"{PRINT_NAME}\" \"{tempFilePath}\"";
                    Process printProcess = new Process();
                    printProcess.StartInfo.FileName = tempFilePath;
                    printProcess.StartInfo.Verb = "print";
                    printProcess.StartInfo.CreateNoWindow = true;
                    printProcess.StartInfo.UseShellExecute = true;
                    printProcess.Start();
                    printProcess.WaitForExit();
                    print.Status = PrintStatus.SUCCESS.ToString();
                }
                catch (Exception)
                {
                    continue;
                }
                finally
                {
                    File.Delete(tempFilePath);
                }
            }

            await _printRepository.UpdateRange(prints);
        }
    }
}
