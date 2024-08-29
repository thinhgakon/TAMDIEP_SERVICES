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

        public MachineJob12(MachineRepository machineRepository, SyncTroughLogger logger)
        {
            _machineRepository = machineRepository;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            //while (Program.SyncTrough12Running == true)
            //{
            //}

            Program.Machine12Running = true;
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await ConnectScaleStationModuleFromController();
            });
            Program.Machine12Running = false;
        }

        public async Task ConnectScaleStationModuleFromController()
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
                    _logger.LogInfo($"Machine Job Ket noi thanh cong MDB 1|2 --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    await MachineJobProcess(machines);
                }
                else
                {
                    _logger.LogInfo($"Machine Job Ket noi that bai MDB 1|2 --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }

                if (client != null)
                {
                    client.Disconnect();
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
            machines = machines.Where(x => x.Code == "1" || x.Code == "2").ToList();

            foreach (var machine in machines)
            {
                try
                {
                    if (machine.StartStatus == "PENDING" && !string.IsNullOrEmpty(machine.CurrentDeliveryCode))
                    {
                        _logger.LogInfo($"Start machine: {machine.Code}");

                        var command = (machine.StartCountingFrom == null || machine.StartCountingFrom == 0) ?
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[!]" :
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[N]{machine.StartCountingFrom}[!]";

                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            _logger.LogInfo($"Khong co du lieu tra ve");
                            continue;
                        }
                        _logger.LogInfo($"Du lieu tra ve: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Start][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "ON";
                            machine.StopStatus = "OFF";

                            await _machineRepository.UpdateMachine(machine);
                            _logger.LogInfo($"Start machine {machine.Code} thanh cong!");
                        }
                        else
                        {
                            _logger.LogInfo($"Tin hieu phan hoi khong thanh cong");
                            continue;
                        }
                    }

                    if (machine.StopStatus == "PENDING")
                    {
                        _logger.LogInfo($"Stop machine: {machine.Code}");

                        client.Send($"*[Stop][MDB][{machine.Code}][!]");
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            _logger.LogInfo($"Khong co du lieu tra ve");
                            continue;
                        }
                        _logger.LogInfo($"Du lieu tra ve: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Stop][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "OFF";
                            machine.StopStatus = "ON";
                            machine.CurrentDeliveryCode = null;

                            await _machineRepository.UpdateMachine(machine);
                            _logger.LogInfo($"Stop machine {machine.Code} thanh cong!");
                        }
                        else
                        {
                            _logger.LogInfo($"Tin hieu phan hoi khong thanh cong");
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
                _logger.LogInfo($"MachineJob12: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }
    }
}
