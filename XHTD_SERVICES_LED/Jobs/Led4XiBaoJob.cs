using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_LED.Devices;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Jobs
{
    [DisallowConcurrentExecution]
    public class Led4XiBaoJob : IJob, IDisposable
    {
        protected readonly LedLogger _logger;
        protected readonly TroughRepository _troughRepository;
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        static TcpClient client = new TcpClient();
        static Stream stream = null;
        static ASCIIEncoding encoding = new ASCIIEncoding();

        protected readonly string PLC_IP_ADDRESS = "192.168.13.210";
        protected readonly int PLC_PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        protected readonly string MACHINE_CODE = MachineCode.MACHINE_XI_BAO_4;

        public Led4XiBaoJob(LedLogger logger, TroughRepository troughRepository, StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
            _logger = logger;
            _troughRepository = troughRepository;
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            await Task.Run(async () =>
            {
                _logger.LogInfo("Thuc hien ket noi machine.");
                await ConnectPLC();
            });
        }

        public async Task ConnectPLC()
        {
            try
            {
                var troughCodes = await _troughRepository.GetActiveXiBaoTroughs();

                var listTroughInThisDevice = new List<string> { "7", "8" };

                troughCodes = troughCodes.Where(x => listTroughInThisDevice.Contains(x)).ToList();

                if (troughCodes == null || troughCodes.Count == 0)
                {
                    return;
                }

                _logger.LogInfo("Bat dau ket noi machine.");
                client = new TcpClient();
                client.ConnectAsync(PLC_IP_ADDRESS, PLC_PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                _logger.LogInfo($"Connected to machine : 3|4");

                await MachineJobProcess(troughCodes);

                if (client != null && client.Connected)
                {
                    client.Close();
                    Thread.Sleep(2000);
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo("Ket noi that bai.");
                _logger.LogInfo(ex.Message);
                _logger.LogInfo(ex.StackTrace);
            }
        }

        public async Task MachineJobProcess(List<string> troughCodes)
        {
            try
            {
                bool anyRunning = false;

                foreach (var troughCode in troughCodes)
                {
                    byte[] data1 = encoding.GetBytes($"*[Count][MX][{troughCode}]#GET[!]");
                    stream.Write(data1, 0, data1.Length);

                    data1 = new byte[BUFFER_SIZE];
                    stream.Read(data1, 0, BUFFER_SIZE);

                    var response = encoding.GetString(data1).Trim();

                    if (response == null || response.Length == 0)
                    {
                        _logger.LogInfo($"Khong co du lieu tra ve");
                        return;
                    }

                    var result = GetInfo(response.Replace("\0", "").Replace("##", "#"));

                    var isRunning = result.Item4 == "Run";
                    var deliveryCode = result.Item3;
                    var countQuantity = Double.TryParse(result.Item2, out double i) ? i : 0;

                    var vehicleCode = "BSX-12345";
                    decimal? planQuantity = 100;
                    string typeProduct = "PCB30";

                    if (countQuantity == 0) continue;

                    if (isRunning)
                    {

                        var order = await _storeOrderOperatingRepository.GetDetail(deliveryCode);
                        if (order != null)
                        {
                            vehicleCode = order.Vehicle;
                            planQuantity = order.SumNumber * 20;
                            typeProduct = !String.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";
                        }

                        DisplayScreenLed($"*[H1][C1]{vehicleCode}[H2][C1][1]{deliveryCode}[2]{typeProduct}[H3][C1][1]DAT[2]{planQuantity}[H4][C1][1]XUAT[2]{countQuantity}[!]");
                        anyRunning = true;
                    }
                }

                if (!anyRunning)
                {
                    DisplayScreenLed($"*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG DEM BAO[H3][C1]MANG XUAT[H4][C1]{troughCodes[1]}        {troughCodes[0]}[!]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"ERROR: {ex.Message}");
            }
        }

        static (string, string, string, string) GetInfo(string input)
        {
            string pattern = @"\*\[Count\]\[MX\]\[(?<gt1>[^\]]+)\]#(?<gt2>[^#]*)#(?<gt3>[^#]+)#(?<gt4>[^#]+)\[!\]";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                return (
                    match.Groups["gt1"].Value,
                    match.Groups["gt2"].Value,
                    match.Groups["gt3"].Value,
                    match.Groups["gt4"].Value
                );
            }

            return (string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public void DisplayScreenLed(string dataCode)
        {
            _logger.LogInfo($"Send led: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(MACHINE_CODE, dataCode))
            {
                _logger.LogInfo($"LED Máy {MACHINE_CODE} - OK");
            }
            else
            {
                _logger.LogInfo($"LED Máy {MACHINE_CODE} - FAILED");
            }
        }

        public void Dispose()
        {
            try
            {
                if (client != null && client?.Connected == true)
                {
                    client.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
