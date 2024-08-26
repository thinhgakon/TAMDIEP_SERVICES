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

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class MachineJob34 : IJob, IDisposable
    {
        private readonly MachineRepository _machineRepository;
        protected readonly SyncTroughLogger _logger;

        static TcpClient client = new TcpClient();
        static Stream stream = null;
        static ASCIIEncoding encoding = new ASCIIEncoding();

        private const string IP_ADDRESS = "192.168.13.210";
        private const int PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        public MachineJob34(MachineRepository machineRepository, SyncTroughLogger logger)
        {
            _machineRepository = machineRepository;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            //while (Program.SyncTrough34Running == true)
            //{
            //}

            Program.Machine34Running = true;
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await ConnectScaleStationModuleFromController();
            });
            Program.Machine34Running = false;
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

                client = new TcpClient();
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);

                if (client.Connected)
                {
                    _logger.LogInfo($"Machine Job Ket noi thanh cong MDB 3|4 --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    stream = client.GetStream();

                    await MachineJobProcess(machines);
                }
                else
                {
                    _logger.LogInfo($"Machine Job Ket noi that bai MDB 3|4 --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }

                if (client != null)
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
                _logger.LogInfo($"Machine Job ERROR IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}: {ex.Message} -- {ex.StackTrace}");
            }
        }

        private async Task MachineJobProcess(List<tblMachine> machines)
        {
            machines = machines.Where(x => x.Code == "3" || x.Code == "4").ToList();

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

                        // 2. send 1
                        byte[] data = encoding.GetBytes(command);
                        stream.Write(data, 0, data.Length);

                        // 3. receive 1
                        data = new byte[BUFFER_SIZE];
                        stream.Read(data, 0, BUFFER_SIZE);

                        var response = encoding.GetString(data).Trim();

                        if (response == null || response.Length == 0)
                        {
                            _logger.LogInfo($"Khong co du lieu tra ve");
                            continue;
                        }
                        _logger.LogInfo($"Du lieu tra ve: {response}");

                        if (response.Contains($"*[Start][MDB][{machine.Code}]#OK#"))
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

                        byte[] data = encoding.GetBytes($"*[Stop][MDB][{machine.Code}][!]");
                        stream.Write(data, 0, data.Length);

                        data = new byte[BUFFER_SIZE];
                        stream.Read(data, 0, BUFFER_SIZE);

                        var response = encoding.GetString(data).Trim();

                        if (response == null || response.Length == 0)
                        {
                            _logger.LogInfo($"Khong co du lieu tra ve");
                            continue;
                        }
                        _logger.LogInfo($"Du lieu tra ve: {response}");

                        if (response.Contains($"*[Stop][MDB][{machine.Code}]#OK#"))
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
                _logger.LogInfo($"MachineJob34: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }
    }
}
