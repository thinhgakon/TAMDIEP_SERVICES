using Autofac;
using log4net;
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
    public class Led34XiBaoJob : IJob, IDisposable
    {
        ILog _logger = LogManager.GetLogger("Led34XiBaoFileAppender");

        protected readonly MachineRepository _machineRepository;
        protected readonly TroughRepository _troughRepository;
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        static TcpClient client = new TcpClient();
        static Stream stream = null;
        static ASCIIEncoding encoding = new ASCIIEncoding();

        protected readonly string PLC_IP_ADDRESS = "192.168.13.210";
        protected readonly int PLC_PORT_NUMBER = 12000;
        private const int BUFFER_SIZE = 1024;

        protected readonly string MACHINE_3_CODE = MachineCode.MACHINE_XI_BAO_3;
        protected readonly string MACHINE_4_CODE = MachineCode.MACHINE_XI_BAO_4;
        protected readonly string MACHINE_MDB_CODE = MachineCode.MACHINE_MDB_1;

        public Led34XiBaoJob(MachineRepository machineRepository, TroughRepository troughRepository, StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
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

            try
            {
                await Task.Run(async () =>
                {
                    WriteLogInfo($"--------------- START JOB REALTIME - IP: {PLC_IP_ADDRESS} ---------------");

                    await ConnectPLC();
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public async Task ConnectPLC()
        {
            try
            {
                WriteLogInfo($"Bat dau ket noi PLC --- IP:{PLC_IP_ADDRESS} - PORT:{PLC_PORT_NUMBER}");

                client = new TcpClient();
                client.ConnectAsync(PLC_IP_ADDRESS, PLC_PORT_NUMBER).Wait(2000);
                stream = client.GetStream();

                WriteLogInfo($"Connected to machine : 3|4");

                WriteLogInfo($"Đọc dữ liệu máng xuất");
                var trough12Codes = new List<string> { "5", "6" };
                await ReadMXData(trough12Codes, MACHINE_3_CODE);

                Thread.Sleep(200);

                var trough34Codes = new List<string> { "7", "8" };
                await ReadMXData(trough34Codes, MACHINE_4_CODE);

                //Thread.Sleep(200);

                //WriteLogInfo($"Đọc dữ liệu máy đếm bao");
                //var machineCodes = new List<string> { "1" };
                //await ReadMDBData(machineCodes);

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
                WriteLogInfo($"Ket noi that bai: {ex.Message} --- {ex.InnerException} -- {ex.StackTrace}");
            }
        }

        public async Task ReadMXData(List<string> troughCodes, string machineCode)
        {
            try
            {
                bool anyRunning = false;
                var sendCode = "";

                foreach (var troughCode in troughCodes)
                {
                    WriteLogInfo($"Máng {troughCode}");

                    var command = $"*[Count][MX][{troughCode}]#GET[!]";

                    WriteLogInfo($"Gửi lệnh: {command}");

                    byte[] data1 = encoding.GetBytes($"{command}");
                    stream.Write(data1, 0, data1.Length);

                    data1 = new byte[BUFFER_SIZE];
                    stream.Read(data1, 0, BUFFER_SIZE);

                    var response = encoding.GetString(data1).Trim();

                    if (response == null || response.Length == 0)
                    {
                        WriteLogInfo($"Khong co du lieu tra ve");
                        return;
                    }
                    else
                    {
                        WriteLogInfo($"Phản hồi: {response}");
                    }

                    var result = GetInfo(response.Replace("\0", "").Replace("##", "#"));

                    var isRunning = result.Item4 == "Run";
                    var deliveryCode = result.Item3;
                    var countQuantity = int.TryParse(result.Item2, out int i) ? i : 0;

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
                            typeProduct = !String.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";
                        }

                        sendCode = $"*[H1][C1]{vehicleCode}[H2][C1][1]{deliveryCode}[2]{typeProduct}[H3][C1][1]DAT[2]{planQuantity}[H4][C1][1]XUAT[2]{countQuantity}[!]";
                        DisplayScreenLed(sendCode, machineCode);
                        anyRunning = true;
                    }
                }

                if (!anyRunning)
                {
                    var machine = await _machineRepository.GetMachineByMachineCode(machineCode);
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
                            DisplayScreenLed(sendCode, machineCode);
                        }
                    }
                    else
                    {
                        //sendCode = $"*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG DEM BAO[H3][C1]MANG XUAT[H4][C1]{troughCodes[1]}        {troughCodes[0]}[!]";
                        sendCode = $"*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG XUAT HANG KHONG DUNG[H3][C1]XIN MOI LAI XE[H4][C1]KIEM TRA VA XAC NHAN DON HANG[!]";
                        DisplayScreenLed(sendCode, machineCode);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"ERROR: {ex.Message}");
            }
        }

        public async Task ReadMDBData(List<string> machineCodes)
        {
            foreach (var machineCode in machineCodes)
            {
                try
                {
                    WriteLogInfo($"Máy {machineCode}");

                    // *[Count][MDB][1]#GET[!]
                    var command = $"*[Count][MDB][{machineCode}]#GET[!]";

                    WriteLogInfo($"Gửi lệnh: {command}");

                    byte[] data1 = encoding.GetBytes($"{command}");
                    stream.Write(data1, 0, data1.Length);

                    data1 = new byte[BUFFER_SIZE];
                    stream.Read(data1, 0, BUFFER_SIZE);

                    var response = encoding.GetString(data1).Trim();

                    if (response == null || response.Length == 0)
                    {
                        WriteLogInfo($"Khong co du lieu tra ve");
                        return;
                    }
                    else
                    {
                        WriteLogInfo($"Phản hồi: {response}");
                    }

                    var result = GetMDBInfo(response.Replace("\0", "").Replace("##", "#"));

                    var isRunning = result.Item4 == "Run";
                    var deliveryCode = result.Item3;
                    var countQuantity = int.TryParse(result.Item2, out int i) ? i : 0;

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
            WriteLogInfo($"Send led khi đọc từ MDB: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(MACHINE_MDB_CODE, dataCode))
            {
                WriteLogInfo($"LED Máy {MACHINE_MDB_CODE} từ MDB - OK");
            }
            else
            {
                WriteLogInfo($"LED Máy {MACHINE_MDB_CODE} từ MDB - FAILED");
            }
        }

        public void DisplayScreenLed(string dataCode, string ledCode)
        {
            WriteLogInfo($"Send led khi đọc từ MX: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(ledCode, dataCode))
            {
                WriteLogInfo($"LED Máy {ledCode} từ MX - OK");
            }
            else
            {
                WriteLogInfo($"LED Máy {ledCode} từ MX - FAILED");
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

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
