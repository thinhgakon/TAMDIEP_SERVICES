﻿using Autofac;
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_LED.Devices;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Jobs
{
    [DisallowConcurrentExecution]
    public class Led34RealtimeJob : IJob, IDisposable
    {
        ILog _logger = LogManager.GetLogger("Led34RealtimeFileAppender");

        private static bool DeviceConnected = false;

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        static SimpleTcpClient client;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string MachineResponse = string.Empty;
        static string TroughResponse = string.Empty;

        public const string IP_ADDRESS = "192.168.13.210";
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 13000;

        private const string MACHINE_1_CODE = "3";
        private const string MACHINE_2_CODE = "4";
        private const string DEFAULT_LED_CODE = "*[H1][C1]VICEM TAM DIEP[H2][C1]HE THONG XUAT HANG KHONG DUNG[H3][C1]XIN MOI LAI XE[H4][C1]KIEM TRA VA XAC NHAN DON HANG[!]";

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        private const int TIME_TO_RESET = 10;

        public Led34RealtimeJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            MachineRepository machineRepository,
            TroughRepository troughRepository
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _machineRepository = machineRepository;
            _troughRepository = troughRepository;
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
                    WriteLogInfo($"--------------- START JOB REALTIME - IP: {IP_ADDRESS} ---------------");

                    await ProcessLedRealtime();
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public async Task ProcessLedRealtime()
        {
            while (true)
            {
                var isConnected = ConnectPlc();

                if (isConnected)
                {

                    await ReadDataFromPlc();
                }

                Thread.Sleep(1000);
            }
        }

        public bool ConnectPlc()
        {
            try
            {
                WriteLogInfo($"Bat dau ket noi PLC --- IP:{IP_ADDRESS} - PORT:{PORT_NUMBER}");

                client = new SimpleTcpClient(IP_ADDRESS, PORT_NUMBER);
                client.Keepalive.EnableTcpKeepAlives = true;
                client.Settings.MutuallyAuthenticate = false;
                client.Settings.AcceptInvalidCertificates = true;
                client.Settings.ConnectTimeoutMs = 2000;
                client.Settings.NoDelay = true;

                client.ConnectWithRetries(2000);

                if (client.IsConnected)
                {
                    WriteLogInfo("Ket noi thanh cong");

                    DeviceConnected = true;
                }

                return DeviceConnected;
            }
            catch (Exception ex)
            {
                WriteLogInfo($"Ket noi that bai: {ex.Message} --- {ex.InnerException} -- {ex.StackTrace}");
                return false;
            }
        }

        public async Task ReadDataFromPlc()
        {
            while (true)
            {
                try
                {
                    client.Events.DataReceived += Trough_DataReceived;
                    Thread.Sleep(200);

                    if (TroughResponse == null || TroughResponse.Length == 0)
                    {
                        WriteLogInfo($"Khong co du lieu tra ve");
                    }
                    else
                    {
                        var dataStr = TroughResponse;

                        WriteLogInfo($"Nhan duoc du lieu: {dataStr}");

                        // Hiển thị LED tại MX
                        await ProcessMXData(dataStr);

                        Program.LastTimeReceivedScaleSocket = DateTime.Now;
                    }

                    if (Program.LastTimeReceivedScaleSocket != null)
                    {
                        timeDiffFromLastReceivedScaleSocket = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedScaleSocket);

                        if (timeDiffFromLastReceivedScaleSocket.TotalSeconds > TIME_TO_RESET)
                        {
                            WriteLogInfo($"Quá {TIME_TO_RESET}s không nhận được tín hiệu => reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedScaleSocket}");

                            Program.LastTimeReceivedScaleSocket = DateTime.Now;

                            if (client != null)
                            {
                                client.Disconnect();
                                client.Dispose();
                                client = null;
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogInfo($@"ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                    if (client != null)
                    {
                        client.Disconnect();
                        client.Dispose();
                        client = null;
                    }

                    break;
                }
                finally
                {
                    TroughResponse = null;
                }

                Thread.Sleep(500);
            }

            await ProcessLedRealtime();
        }

        public async Task ProcessMXData(string dataStr)
        {
            var result = GetInfo(dataStr.Replace("\0", "").Replace("##", "#"), "MX");

            var isRunning = result.Item4 == "Run";
            var deliveryCode = result.Item3;
            var countQuantity = Double.TryParse(result.Item2, out double i) ? i : 0;
            var troughCode = result.Item1;

            var vehicleCode = "BSX-12345";
            var planQuantity = 100;
            string typeProduct = "PCB30";

            if (countQuantity == 0)
            {
                return;
            }

            if (isRunning)
            {
                var machine = await _machineRepository.GetMachineByTroughCode(troughCode);

                if (machine == null)
                {
                    WriteLogInfo($"Chua cau hinh active machine (TblMachineTrough co status = true) cho mang xuat troughCode={troughCode}");
                    return;
                }

                var isActiveInMachine = await _troughRepository.IsTroughActiveInAnyMachine(troughCode);

                if (!isActiveInMachine)
                {
                    DisplayScreenLed(DEFAULT_LED_CODE, machine.Code);
                    return;
                }

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
                else
                {
                    WriteLogInfo($"Khong tim thay don hang {deliveryCode}");
                    return;
                }

                var sendCode = $"*[H1][C1]{vehicleCode}[H2][C1][1]{deliveryCode}[2]{typeProduct}[H3][C1][1]DAT[2]{planQuantity}[H4][C1][1]XUAT[2]{countQuantity}[!]";

                DisplayScreenLed(sendCode, machine.Code);
            }
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

        public void DisplayScreenLed(string dataCode, string ledCode)
        {
            WriteLogInfo($"Send led: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(ledCode, dataCode))
            {
                WriteLogInfo($"LED Máy {ledCode} - OK");
            }
            else
            {
                WriteLogInfo($"LED Máy {ledCode} - FAILED");
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
