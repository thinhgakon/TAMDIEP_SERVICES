using System;
using System.Drawing.Printing;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_INIT.Models.Values;
using XHTD_SERVICES.Helper;
using System.Text;

namespace XHTD_SERVICES_GATEWAY_PRINT.Jobs
{
    [DisallowConcurrentExecution]
    public class PrintJob : IJob
    {
        private readonly PrintRepository _printRepository;
        private readonly string PRINT_NAME = "";

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
                try
                {
                    var response = HttpRequest.PrintInvoice(print.ErpOrderId);

                    using (MemoryStream stream = new MemoryStream(response.RawBytes))
                    {
                        PdfDocument document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

                        PrintDocument printDocument = new PrintDocument
                        {
                            PrinterSettings = new PrinterSettings
                            {
                                PrinterName = PRINT_NAME
                            }
                        };

                        int currentPageIndex = 0;

                        printDocument.PrintPage += (sender, e) =>
                        {
                            if (currentPageIndex < document.PageCount)
                            {
                                var page = document.Pages[currentPageIndex];

                                XGraphics gfx = XGraphics.FromPdfPage(page);
                                gfx.DrawImage(XImage.FromStream(stream), 0, 0, e.PageBounds.Width, e.PageBounds.Height);
                                gfx.Dispose();

                                currentPageIndex++;
                                e.HasMorePages = currentPageIndex < document.PageCount;
                            }
                        };

                        printDocument.Print();
                    }
                    print.Status = PrintStatus.SUCCESS.ToString();
                }
                catch (Exception)
                {
                    continue;
                }
            }

            await _printRepository.UpdateRange(prints);
        }
    }
}
