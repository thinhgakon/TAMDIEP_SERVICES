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
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_LED.Devices;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Jobs
{
    [DisallowConcurrentExecution]
    public class Led1XiBaoNewJob : IJob, IDisposable
    {
        private static bool DeviceConnected = false;

        protected readonly LedLogger _logger;

        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;

        public const string PLC_IP_ADDRESS = "192.168.13.189";
        private const int PLC_PORT_NUMBER = 13000;
        private const int BUFFER_SIZE = 1024;

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        public Led1XiBaoNewJob(
            LedLogger syncTroughLogger
            )
        {
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
                client.ConnectAsync(PLC_IP_ADDRESS, PLC_PORT_NUMBER).Wait(2000);
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
