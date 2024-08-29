using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_SYNC_TROUGH.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES_SYNC_TROUGH.Models.Values;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;
using System.Text;
using System.Net.Sockets;
using System.IO;
using XHTD_SERVICES.Data.Models.Values;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices.ComTypes;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class SyncTroughJobNew12 : IJob, IDisposable
    {
        private static bool DeviceConnected = false;
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly SyncTroughLogger _logger;

        Thread controlPLCThread;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_TROUGH_ACTIVE";

        private static bool isActiveService = true;

        private int TimeInterVal = 2000;

        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;
        private readonly Notification _notification;
        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";
        private readonly string SEND_TO_RECEIVED_SCALE_CODE = "ww";

        public const string IP_ADDRESS = "192.168.13.210";

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        public SyncTroughJobNew12(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            MachineRepository machineRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            SyncTroughLogger syncTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _machineRepository = machineRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _logger = syncTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                AuthenticateScaleStationModuleFromController();
            });
        }

        public void AuthenticateScaleStationModuleFromController()
        {
            while (true)
            {
                var isConnected = ConnectScaleStationModuleFromController();

                if (isConnected)
                {

                    controlPLCThread = new Thread(() =>
                    {
                        ProcessPendingStatusPlc();
                    });
                    controlPLCThread.IsBackground = true;
                    controlPLCThread.Start();

                    ReadDataFromController();
                }

                Thread.Sleep(1000);
            }
        }

        public bool ConnectScaleStationModuleFromController()
        {
            try
            {
                _logger.LogInfo("Thuc hien ket noi scale socket");
                client = new TcpClient();

                // 1. connect
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                _logger.LogInfo("Ket noi thanh cong");

                DeviceConnected = true;

                //var data = encoding.GetBytes(START_CONNECTION_STR);
                //stream.Write(data, 0, data.Length);

                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Ket noi that bai: {ex.Message}");
                return false;
            }
        }

        public void ReadDataFromController()
        {
            while (true)
            {
                try
                {
                    // send
                    //byte[] data = encoding.GetBytes(SEND_TO_RECEIVED_SCALE_CODE);
                    //stream.Write(data, 0, data.Length);

                    // receive
                    byte[] data = new byte[BUFFER_SIZE];
                    stream.Read(data, 0, BUFFER_SIZE);
                    //stream.ReadAsync(data, 0, BUFFER_SIZE).Wait(1000);

                    var dataStr = encoding.GetString(data);

                    _logger.LogInfo($"Nhan tin hieu can: {dataStr}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
                    if (stream != null) stream.Close();
                    if (client != null) client.Close();

                    break;
                }

                Thread.Sleep(500);
            }

            AuthenticateScaleStationModuleFromController();
        }

        public void ProcessPendingStatusPlc()
        {
            while (true)
            {
                Console.WriteLine("process pending plc");
                Thread.Sleep(1000);
            }
        }

        public void Dispose()
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SyncTroughJob12: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }
    }
}
