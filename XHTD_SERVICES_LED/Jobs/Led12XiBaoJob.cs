using Autofac;
using log4net;
using Quartz;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
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
    public class Led12XiBaoJob : IJob, IDisposable
    {
        ILog _logger = LogManager.GetLogger("Led12XiBaoFileAppender");

        protected readonly MachineRepository _machineRepository;
        protected readonly TroughRepository _troughRepository;
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        static SimpleTcpClient client;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string MachineResponse = string.Empty;
        static string TroughResponse = string.Empty;

        protected readonly string PLC_IP_ADDRESS = "192.168.13.189";
        protected readonly int PLC_PORT_NUMBER = 12000;
        private const int BUFFER_SIZE = 1024;

        protected readonly string MACHINE_1_CODE = MachineCode.MACHINE_XI_BAO_1;
        protected readonly string MACHINE_2_CODE = MachineCode.MACHINE_XI_BAO_2;
        protected readonly string MACHINE_MDB_CODE = MachineCode.MACHINE_MDB_1;
        protected readonly string DEFAULT_LED_CODE = "*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG XUAT HANG KHONG DUNG[H3][C1]XIN MOI LAI XE[H4][C1]KIEM TRA VA XAC NHAN DON HANG[!]";

        public Led12XiBaoJob(MachineRepository machineRepository, TroughRepository troughRepository, StoreOrderOperatingRepository storeOrderOperatingRepository)
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
                    WriteLogInfo($"--------------- START JOB - IP: {PLC_IP_ADDRESS} ---------------");

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

                client = new SimpleTcpClient(PLC_IP_ADDRESS, PLC_PORT_NUMBER);
                client.Keepalive.EnableTcpKeepAlives = true;
                client.Settings.MutuallyAuthenticate = false;
                client.Settings.AcceptInvalidCertificates = true;
                client.Settings.ConnectTimeoutMs = 2000;
                client.Settings.NoDelay = true;

                client.ConnectWithRetries(2000);

                if (client.IsConnected)
                {
                    WriteLogInfo($"Connected to machine : 1|2");

                    WriteLogInfo($"Đọc dữ liệu máng xuất");
                    var trough12Codes = await _troughRepository.GetActiveTroughInMachine(MACHINE_1_CODE);
                    trough12Codes = trough12Codes.Where(trough => trough != "9" && trough != "10").ToList();

                    if (trough12Codes != null && trough12Codes.Count > 0)
                    {
                        await ReadMXData(trough12Codes, MACHINE_1_CODE);
                    }
                    else
                    {
                        DisplayScreenLed(DEFAULT_LED_CODE, MACHINE_1_CODE);
                    }

                    Thread.Sleep(200);

                    WriteLogInfo($"Đọc dữ liệu máy đếm bao");
                    var trough34Codes = await _troughRepository.GetActiveTroughInMachine(MACHINE_2_CODE);
                    trough34Codes = trough34Codes.Where(trough => trough != "9" && trough != "10").ToList();

                    if (trough34Codes != null && trough34Codes.Count > 0)
                    {
                        await ReadMXData(trough34Codes, MACHINE_2_CODE);
                    }
                    else
                    {
                        DisplayScreenLed(DEFAULT_LED_CODE, MACHINE_2_CODE);
                    }

                    Thread.Sleep(200);

                    var machineCodes = new List<string> { "1" };
                    await ReadMDBData(machineCodes);
                }
                else
                {
                    WriteLogInfo($"Trough Job MDB 1|2: Ket noi that bai --- IP: {PLC_IP_ADDRESS} --- PORT: {PLC_PORT_NUMBER}");
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"Ket noi that bai: {ex.Message} --- {ex.InnerException} -- {ex.StackTrace}");
            }
            finally
            {
                if (client != null)
                {
                    client.Disconnect();
                }
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

                    client.Send(command);
                    client.Events.DataReceived += Trough_DataReceived;
                    Thread.Sleep(200);

                    if (TroughResponse == null || TroughResponse.Length == 0)
                    {
                        WriteLogInfo($"2. Không có phản hồi");
                        continue;
                    }
                    else
                    {
                        WriteLogInfo($"2. Phản hồi: {TroughResponse}");
                    }

                    var result = GetInfo(TroughResponse.Replace("\0", "").Replace("##", "#"));

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

                            double? orderNetWeight = 50;
                            if (order.NetWeight != null && order.NetWeight != 0)
                            {
                                orderNetWeight = order.NetWeight;
                            }
                            
                            planQuantity = (int)((double)order.SumNumber * 1000 / orderNetWeight);
                            
                            if (!String.IsNullOrEmpty(order.ItemAlias))
                            {
                                typeProduct = order.ItemAlias;
                            }
                            else
                            {
                                typeProduct = !String.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";
                            }
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

                            double? orderNetWeight = 50;
                            if (order.NetWeight != null && order.NetWeight != 0)
                            {
                                orderNetWeight = order.NetWeight;
                            }

                            var planQuantity = 0;
                            
                            planQuantity = (int)((double)order.SumNumber * 1000 / orderNetWeight);
                            
                            var typeProduct = "---";
                            if (!String.IsNullOrEmpty(order.ItemAlias))
                            {
                                typeProduct = order.ItemAlias;
                            }
                            else
                            {
                                typeProduct = !String.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";
                            }

                            decimal? exportedNumber = 0;

                            if(order.NetWeight != null && order.NetWeight != 0)
                            {
                                exportedNumber = order.ExportedNumber != null ? order.ExportedNumber * 1000 / (decimal)order.NetWeight : 0;
                            }
                            else
                            {
                                exportedNumber = order.ExportedNumber != null ? order.ExportedNumber * 1000 / 50 : 0;
                            }

                            sendCode = $"*[H1][C1]{vehicleCode}[H2][C1][1]{machine.CurrentDeliveryCode}[2]{typeProduct}[H3][C1][1]DAT[2]{planQuantity}[H4][C1][1]XUAT[2]{exportedNumber}[!]";
                            DisplayScreenLed(sendCode, machineCode);
                        }
                    }
                    else
                    {
                        DisplayScreenLed(DEFAULT_LED_CODE, machineCode);
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

                    client.Send(command);
                    client.Events.DataReceived += Machine_DataReceived;
                    Thread.Sleep(200);

                    if (MachineResponse == null || MachineResponse.Length == 0)
                    {
                        WriteLogInfo($"2. Không có phản hồi");
                        continue;
                    }
                    else
                    {
                        WriteLogInfo($"2. Phản hồi: {MachineResponse}");
                    }

                    var result = GetMDBInfo(MachineResponse.Replace("\0", "").Replace("##", "#"));

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

                            if (order.NetWeight != null && order.NetWeight != 0)
                            {
                                planQuantity = (int)((double)order.SumNumber * 1000 / order.NetWeight);
                            }
                            else
                            {
                                planQuantity = (int)(order.SumNumber * 1000 / 50);
                            }

                            if (!String.IsNullOrEmpty(order.ItemAlias))
                            {
                                typeProduct = order.ItemAlias;
                            }
                            else
                            {
                                typeProduct = !String.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";
                            }
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
                    WriteLogInfo($"ReadMDBData ERROR: Machine {machineCode} -- {ex.Message} --- {ex.StackTrace}");
                    client.Disconnect();
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

        public void DisplayScreenLed(string dataCode, string machineCode)
        {
            WriteLogInfo($"Send led khi đọc từ MX: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(machineCode, dataCode))
            {
                WriteLogInfo($"LED Máy {machineCode} từ MX - OK");
            }
            else
            {
                WriteLogInfo($"LED Máy {machineCode} từ MX - FAILED");
            }
        }

        private void Machine_DataReceived(object sender, DataReceivedEventArgs e)
        {
            MachineResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        private void Trough_DataReceived(object sender, DataReceivedEventArgs e)
        {
            TroughResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        public void Dispose()
        {
            try
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SyncTroughJob12: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
