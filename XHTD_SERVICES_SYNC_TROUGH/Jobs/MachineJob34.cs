using Quartz;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using System.Threading;
using System.Timers;
using System.Collections.Generic;
using XHTD_SERVICES.Data.Entities;
using SuperSimpleTcp;
using XHTD_SERVICES.Helper;
using System.Data.Entity;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class MachineJob34 : IJob, IDisposable
    {
        private readonly MachineRepository _machineRepository;
        protected readonly Notification _notification;
        protected readonly SyncTroughLogger _logger;

        static SimpleTcpClient client;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string MachineResponse = string.Empty;

        private const string IP_ADDRESS = "192.168.13.210";
        private const int PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        private const string MACHINE_1_CODE = "3";
        private const string MACHINE_2_CODE = "4";

        public MachineJob34(MachineRepository machineRepository, Notification notification, SyncTroughLogger logger)
        {
            _machineRepository = machineRepository;
            _notification = notification;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await ConnectPLC();
            });
        }

        public async Task ConnectPLC()
        {
            try
            {
                var machines = await _machineRepository.GetPendingMachine();
                if (machines == null || machines.Count == 0)
                {
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
                    _logger.LogInfo($"Machine Job Ket noi thanh cong MDB {MACHINE_1_CODE}|{MACHINE_2_CODE} --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    await MachineJobProcess(machines);
                }
                else
                {
                    _logger.LogInfo($"Machine Job Ket noi that bai MDB {MACHINE_1_CODE}|{MACHINE_2_CODE} --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }

                if (client != null)
                {
                    client.Dispose();
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Machine Job ERROR IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}: {ex.Message} -- {ex.StackTrace}");
            }
        }

        private async Task MachineJobProcess(List<tblMachine> machines)
        {
            machines = machines.Where(x => x.Code == MACHINE_1_CODE || x.Code == MACHINE_2_CODE).ToList();

            foreach (var machine in machines)
            {
                try
                {
                    if (machine.StartStatus == "PENDING" && !string.IsNullOrEmpty(machine.CurrentDeliveryCode))
                    {
                        _logger.LogInfo($"Start machine code: {machine.Code} - msgh: {machine.CurrentDeliveryCode}============================================");

                        var command = (machine.StartCountingFrom == null || machine.StartCountingFrom == 0) ?
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[!]" :
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[N]{machine.StartCountingFrom}[!]";

                        _logger.LogInfo($"1. Gửi lệnh: {command}");
                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            _logger.LogInfo($"2. Không có phản hồi");
                            continue;
                        }
                        _logger.LogInfo($"2. Phản hồi: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Start][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "ON";
                            machine.StopStatus = "OFF";

                            await _machineRepository.UpdateMachine(machine);

                            _logger.LogInfo($"2.1. Start thành công");

                            using (var db = new XHTD_Entities())
                            {
                                var callToTrough = await db.tblCallToTroughs.FirstOrDefaultAsync(x => x.DeliveryCode == machine.CurrentDeliveryCode && x.IsDone == false);
                                if (callToTrough == null) continue;

                                var trough = await db.tblTroughs.FirstOrDefaultAsync(x => x.Code == callToTrough.Machine);
                                if (trough == null) continue;

                                trough.DeliveryCodeCurrent = machine.CurrentDeliveryCode;
                                await db.SaveChangesAsync();
                            }

                            SendNotificationAPI(string.Empty, machine.Code, machine.StartStatus, machine.StopStatus);
                            SendMachineStartNotification(machine.Code, string.Empty, machine.CurrentDeliveryCode, string.Empty);
                        }
                        else
                        {
                            _logger.LogInfo($"2.1. Start thất bại");
                            continue;
                        }
                    }

                    if (machine.StopStatus == "PENDING")
                    {
                        _logger.LogInfo($"Stop machine code: {machine.Code} ============================================");

                        var command = $"*[Stop][MDB][{machine.Code}][!]";

                        _logger.LogInfo($"1. Gửi lệnh: {command}");
                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            _logger.LogInfo($"2. Không có phản hồi");
                            continue;
                        }
                        _logger.LogInfo($"2. Phản hồi: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Stop][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "OFF";
                            machine.StopStatus = "ON";
                            machine.CurrentDeliveryCode = null;

                            await _machineRepository.UpdateMachine(machine);

                            _logger.LogInfo($"2.1. Stop thành công");

                            SendNotificationAPI(string.Empty, machine.Code, machine.StartStatus, machine.StopStatus);
                            SendMachineStopNotification(machine.Code, string.Empty, machine.CurrentDeliveryCode, string.Empty);
                        }
                        else
                        {
                            _logger.LogInfo($"2.1. Stop thất bại");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"MachineJobProcess ERROR: Code={machine.Code} --- {ex.Message} --- {ex.StackTrace}");
                }
            }
        }

        private void Machine_DataReceived(object sender, DataReceivedEventArgs e)
        {
            MachineResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        public void SendNotificationAPI(string machineType, string machineCode, string startStatus, string stopStatus)
        {
            try
            {
                _notification.SendMachineNotification(machineType, machineCode, startStatus, stopStatus);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationAPI Machine {MACHINE_1_CODE}|{MACHINE_2_CODE} Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendMachineStartNotification(string machineCode, string troughCode, string deliveryCode, string vehicle)
        {
            try
            {
                _notification.SendTroughStartData(machineCode, troughCode, deliveryCode, vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendMachineStartNotification Machine {MACHINE_1_CODE}|{MACHINE_2_CODE} Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendMachineStopNotification(string machineCode, string troughCode, string deliveryCode, string vehicle)
        {
            try
            {
                _notification.SendTroughStopData(machineCode, troughCode, deliveryCode, vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendMachineStopNotification Machine {MACHINE_1_CODE}|{MACHINE_2_CODE} Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
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
                _logger.LogInfo($"MachineJob {MACHINE_1_CODE}|{MACHINE_2_CODE}: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }
    }
}
