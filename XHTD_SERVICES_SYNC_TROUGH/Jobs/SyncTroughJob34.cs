using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Helper;
using System.Threading;
using System.Text;
using XHTD_SERVICES.Data.Models.Values;
using System.Text.RegularExpressions;
using SuperSimpleTcp;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class SyncTroughJob34 : IJob, IDisposable
    {
        ILog _logger = LogManager.GetLogger("Sync34FileAppender");

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;
        protected readonly MachineRepository _machineRepository;
        protected readonly TroughRepository _troughRepository;
        protected readonly CallToTroughRepository _callToTroughRepository;
        protected readonly SystemParameterRepository _systemParameterRepository;
        protected readonly Notification _notification;

        private const string IP_ADDRESS = "192.168.13.210";
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 11000;
        private int TimeInterVal = 2000;
        private List<string> machineCodes = new List<string>() { "3", "4" };
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static SimpleTcpClient client;
        static string MachineResponse = string.Empty;
        static string TroughResponse = string.Empty;

        public SyncTroughJob34(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            MachineRepository machineRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            Notification notification
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _machineRepository = machineRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _notification = notification;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await SyncTroughProcess();
            });
        }

        public async Task SyncTroughProcess()
        {
            try
            {
                var troughCodes = new List<string>();

                foreach (var machineCode in machineCodes)
                {
                    var troughInMachine = await _troughRepository.GetActiveTroughInMachine(machineCode);
                    troughCodes.AddRange(troughInMachine);
                }

                if (troughCodes == null || troughCodes.Count == 0)
                {
                    WriteLogInfo($"Trough Job MDB 3|4: Khong tim thay mang xuat --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    return;
                }

                client = new SimpleTcpClient(IP_ADDRESS, PORT_NUMBER);
                client.Keepalive.EnableTcpKeepAlives = true;
                client.Settings.MutuallyAuthenticate = false;
                client.Settings.AcceptInvalidCertificates = true;
                client.Settings.ConnectTimeoutMs = 2000;
                client.Settings.NoDelay = true;

                client.ConnectWithRetries(2000);

                if (client.IsConnected)
                {
                    WriteLogInfo($"Trough Job MDB 3|4: Ket noi thanh cong --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    await ReadDataFromMachine(machineCodes);
                    await ReadDataFromTrough(troughCodes);
                }
                else
                {
                    WriteLogInfo($"Trough Job MDB 3|4: Ket noi that bai --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"Trough Job MDB 3|4: ERROR --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}: {ex.Message} -- {ex.StackTrace}");
            }
            finally
            {
                if (client != null)
                {
                    client.Disconnect();
                }
            }
        }

        public async Task ReadDataFromMachine(List<string> machineCodes)
        {
            foreach (var machineCode in machineCodes)
            {
                try
                {
                    WriteLogInfo($"Đếm đầu máng: {machineCode} ============================================================");

                    var command = $"*[Count][MDB][{machineCode}]#GET[!]";

                    WriteLogInfo($"1. Gửi lệnh: {command}");
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

                    var machineResult = GetInfo(MachineResponse.Replace("\0", "").Replace("##", "#"), "MDB");
                    SendPLCNotification("PLC", machineCode, machineResult.Item4, null);

                    var status = machineResult.Item4 == "Run" ? "True" : "False";
                    var firstSensorQuantity = (Double.TryParse(machineResult.Item2, out double j) ? j : 0);
                    var deliveryCode = machineResult.Item3;

                    if (firstSensorQuantity == 0)
                    {
                        continue;
                    }

                    if (status == "False")
                    {
                        continue;
                    }

                    WriteLogInfo($"3. Cập nhật dữ liệu đầu máng: msgh={deliveryCode} -- firstSensor={firstSensorQuantity}");
                    await _troughRepository.UpdateMachineSensor(deliveryCode, firstSensorQuantity, DateTime.Now, DateTime.Now);
                    SendNotificationAPI("XI_BAO", deliveryCode, machineCode, null, (int?)firstSensorQuantity, null);
                }
                catch (Exception ex)
                {
                    WriteLogInfo($"ReadDataFromMachine ERROR: Machine {machineCode} -- {ex.Message} --- {ex.StackTrace}");
                    client.Disconnect();
                }
            }
        }

        public async Task ReadDataFromTrough(List<string> troughCodes)
        {
            foreach (var troughCode in troughCodes)
            {
                try
                {
                    WriteLogInfo($"Đếm cuối máng: {troughCode} ============================================================");

                    var troughInfo = await _troughRepository.GetDetail(troughCode);
                    if (troughInfo == null)
                    {
                        WriteLogInfo($"Mang khong ton tai: {troughCode} => Thoat");
                        continue;
                    }

                    var command = $"*[Count][MX][{troughCode}]#GET[!]";

                    WriteLogInfo($"1. Gửi lệnh: {command}");

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

                    var result = GetInfo(TroughResponse.Replace("\0", "").Replace("##", "#"), "MX");

                    var status = result.Item4 == "Run" ? "True" : "False";
                    var deliveryCode = result.Item3;
                    var countQuantity = (Double.TryParse(result.Item2, out double i) ? i : 0);
                    if (countQuantity == 0)
                    {
                        continue;
                    }

                    var planQuantity = 100;
                    var troughCodeReturn = result.Item1;
                    if (status == "True")
                    {
                        WriteLogInfo($"Mang {troughCodeReturn} dang xuat hang deliveryCode {deliveryCode}");

                        WriteLogInfo($"3. Cập nhật dữ liệu cuối máng: msgh={deliveryCode} -- trough: {troughCodeReturn} -- countQuantity={countQuantity}");
                        await _troughRepository.UpdateTroughSensor(troughCodeReturn, deliveryCode, countQuantity, planQuantity, DateTime.Now, DateTime.Now);

                        var trough = await _troughRepository.GetDetail(troughCode);
                        var machine = await _machineRepository.GetMachineByMachineCode(trough.Machine);

                        if (machine.StartStatus == "ON" && machine.StopStatus == "OFF")
                        {
                            await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DANG_LAY_HANG);
                            SendNotificationAPI("XI_BAO", deliveryCode, machine.Code, troughCode, null, (int?)countQuantity);
                        }
                    }
                    else
                    {
                        //TODO: xét thêm trường hợp đang xuất dở đơn mà chuyển qua máng khác thì không update được lại trạng thái Đang lấy hàng
                        WriteLogInfo($"Mang {troughCodeReturn} dang nghi");

                        WriteLogInfo($"Reset trough troughCode {troughCodeReturn}");
                        //await _troughRepository.ResetTrough(troughCode);
                        await _troughRepository.UpdateTrough(troughCodeReturn, null, 0, 0, 0);
                    }
                }
                catch (Exception ex)
                {
                    WriteLogInfo($"ReadDataFromTrough ERROR: Trough {troughCode} -- {ex.Message} --- {ex.StackTrace}");
                    client.Disconnect();
                }
            }
        }

        public void SendPLCNotification(string machineType, string machineCode, string startStatus, string stopStatus)
        {
            _notification.SendMachineNotification(machineType, machineCode, startStatus, stopStatus);
        }

        private void SendNotificationAPI(string troughType, string deliveryCode, string machineCode, string troughCode, int? firstQuantity, int? lastQuantity)
        {
            _notification.SendTroughData(troughType, deliveryCode, machineCode, troughCode, firstQuantity, lastQuantity);
        }

        private void Machine_DataReceived(object sender, DataReceivedEventArgs e)
        {
            MachineResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        private void Trough_DataReceived(object sender, DataReceivedEventArgs e)
        {
            TroughResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        static (string, string, string, string) GetInfo(string input, string type)
        {
            string pattern = $@"\*\[Count\]\[{type}\]\[(?<gt1>[^\]]+)\]#(?<gt2>[^#]*)#(?<gt3>[^#]+)#(?<gt4>[^#]+)\[!\]";
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
                WriteLogInfo($"SyncTroughJob34: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
