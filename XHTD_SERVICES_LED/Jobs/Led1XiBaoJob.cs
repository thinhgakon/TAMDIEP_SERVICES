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
    public class Led1XiBaoJob : IJob, IDisposable
    {
        protected readonly LedLogger _logger;
        protected readonly MachineRepository _machineRepository;
        protected readonly TroughRepository _troughRepository;
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        static TcpClient client = new TcpClient();
        static Stream stream = null;
        static ASCIIEncoding encoding = new ASCIIEncoding();

        protected readonly string PLC_IP_ADDRESS = "192.168.13.189";
        protected readonly int PLC_PORT_NUMBER = 12000;
        private const int BUFFER_SIZE = 1024;

        protected readonly string MACHINE_CODE = MachineCode.MACHINE_XI_BAO_1;
        protected readonly string MACHINE_MDB_CODE = MachineCode.MACHINE_MDB_1;

        public Led1XiBaoJob(LedLogger logger, MachineRepository machineRepository, TroughRepository troughRepository, StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
            _logger = logger;
            _machineRepository = machineRepository;
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
                _logger.LogInfo("Bat dau ket noi machine.");
                client = new TcpClient();
                client.ConnectAsync(PLC_IP_ADDRESS, PLC_PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                _logger.LogInfo($"Connected to machine : 1|2");

                var troughCodes = new List<string> { "1", "2" };
                await ReadMXData(troughCodes);

                var machineCodes = new List<string> { "1" };
                await ReadMDBData(machineCodes);

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

        public async Task ReadMXData(List<string> troughCodes)
        {
            try
            {
                bool anyRunning = false;
                var sendCode = "";

                foreach (var troughCode in troughCodes)
                {
                    var command = $"*[Count][MX][{troughCode}]#GET[!]";
                    byte[] data1 = encoding.GetBytes($"{command}");
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
                    var planQuantity = 100;
                    string typeProduct = "PCB30";

                    if (countQuantity == 0) continue;

                    if (isRunning)
                    {
                        var order = await _storeOrderOperatingRepository.GetDetail(deliveryCode);
                        if (order != null) 
                        {
                            vehicleCode = order.Vehicle;
                            planQuantity = (int)(order.SumNumber * 20);
                            typeProduct = !String.IsNullOrEmpty(order.TypeProduct)? order.TypeProduct : "---";
                        }

                        sendCode = $"*[H1][C1]{vehicleCode}[H2][C1][1]{deliveryCode}[2]{typeProduct}[H3][C1][1]DAT[2]{planQuantity}[H4][C1][1]XUAT[2]{countQuantity}[!]";
                        DisplayScreenLed(sendCode);
                        anyRunning = true;
                    }
                }

                if (!anyRunning)
                {
                    var machine = await _machineRepository.GetMachineByMachineCode(MACHINE_CODE);
                    if (machine.StartStatus == "ON" && machine.StopStatus == "OFF" && !string.IsNullOrEmpty(machine.CurrentDeliveryCode))
                    {
                        var order = await _storeOrderOperatingRepository.GetDetail(machine.CurrentDeliveryCode);
                        if (order != null)
                        {
                            var vehicleCode = order.Vehicle;
                            var planQuantity = (int)(order.SumNumber * 20);
                            var typeProduct = !string.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";
                            var exportedNumber = order.ExportedNumber != null ? order.ExportedNumber * 20 : 0;

                            sendCode = $"*[H1][C1]{vehicleCode}[H2][C1][1]{machine.CurrentDeliveryCode}[2]{typeProduct}[H3][C1][1]DAT[2]{planQuantity}[H4][C1][1]XUAT[2]{exportedNumber}[!]";
                            DisplayScreenLed(sendCode);
                        }
                    }
                    else
                    {
                        sendCode = $"*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG DEM BAO[H3][C1]MANG XUAT[H4][C1]{troughCodes[1]}        {troughCodes[0]}[!]";
                        DisplayScreenLed(sendCode);
                    }
                }
            }
            catch (Exception ex)         
            {
                _logger.LogInfo($"ERROR: {ex.Message}");
            }
        }

        public async Task ReadMDBData(List<string> machineCodes)
        {
            foreach (var machineCode in machineCodes)
            {
                try
                {
                    // *[Count][MDB][1]#GET[!]
                    var command = $"*[Count][MDB][{machineCode}]#GET[!]";
                    byte[] data1 = encoding.GetBytes($"{command}");
                    stream.Write(data1, 0, data1.Length);

                    data1 = new byte[BUFFER_SIZE];
                    stream.Read(data1, 0, BUFFER_SIZE);

                    var response = encoding.GetString(data1).Trim();

                    if (response == null || response.Length == 0)
                    {
                        _logger.LogInfo($"Khong co du lieu tra ve");
                        return;
                    }

                    var result = GetMDBInfo(response.Replace("\0", "").Replace("##", "#"));

                    var isRunning = result.Item4 == "Run";
                    var deliveryCode = result.Item3;
                    var countQuantity = Double.TryParse(result.Item2, out double i) ? i : 0;

                    var vehicleCode = "BSX-12345";
                    var planQuantity = 100;
                    string typeProduct = "PCB30";

                    if (isRunning)
                    {
                        var order = await _storeOrderOperatingRepository.GetDetail(deliveryCode);
                        if (order != null)
                        {
                            vehicleCode = order.Vehicle;
                            planQuantity = (int)(order.SumNumber * 20);
                            typeProduct = !String.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";

                        }

                        DisplayScreenMDBLed($"*[H1][C1]{typeProduct}[H2][C1]{planQuantity - countQuantity}[H3][C1]---[H4][Cy]---[!]");
                    }
                    else
                    {

                        DisplayScreenMDBLed($"*[H1][C1]MDB[H2][C1]---[H3][C1]---[H4][Cy]---[!]");
                    }
                }
                catch (Exception ex)
                {
                }
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

        static (string, string, string, string) GetMDBInfo(string input)
        {
            string pattern = @"\*\[Count\]\[MDB\]\[(?<gt1>[^\]]+)\]#(?<gt2>[^#]*)#(?<gt3>[^#]+)#(?<gt4>[^#]+)\[!\]";
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

        public void DisplayScreenMDBLed(string dataCode)
        {
            _logger.LogInfo($"Send led: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(MACHINE_MDB_CODE, dataCode))
            {
                _logger.LogInfo($"LED Máy {MACHINE_MDB_CODE} - OK");
            }
            else
            {
                _logger.LogInfo($"LED Máy {MACHINE_MDB_CODE} - FAILED");
            }
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
