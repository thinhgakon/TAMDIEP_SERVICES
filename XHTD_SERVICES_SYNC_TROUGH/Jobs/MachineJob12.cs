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

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class MachineJob12 : IJob, IDisposable
    {
        private readonly MachineRepository _machineRepository;
        protected readonly SyncTroughLogger _logger;

        static SimpleTcpClient client;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string MachineResponse = string.Empty;

        private const string IP_ADDRESS = "192.168.13.189";
        private const int PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        private const string MACHINE_1_CODE = "1";
        private const string MACHINE_2_CODE = "2";

        public MachineJob12(MachineRepository machineRepository, SyncTroughLogger logger)
        {
            _machineRepository = machineRepository;
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
