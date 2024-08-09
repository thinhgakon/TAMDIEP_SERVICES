using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    public class MachineJob12 : IJob
    {
        private readonly MachineRepository _machineRepository;
        protected readonly SyncTroughLogger _logger;

        static TcpClient client = new TcpClient();
        static Stream stream = null;
        static ASCIIEncoding encoding = new ASCIIEncoding();

        private const string IP_ADDRESS = "192.168.13.189";
        private const int PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        public MachineJob12(MachineRepository machineRepository,SyncTroughLogger logger)
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
                await ConnectScaleStationModuleFromController();
            });
        }

        public async Task ConnectScaleStationModuleFromController()
        {
            _logger.LogInfo("Thuc hien ket noi.");
            try
            {
                _logger.LogInfo("Bat dau ket noi.");
                client = new TcpClient();
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                _logger.LogInfo($"Connected to machine : 1|2");
                await MachineJobProcess();
                client.Close();
                stream.Close();
            }
            catch (Exception ex)
            {
                _logger.LogInfo("Ket noi that bai.");
                _logger.LogInfo(ex.Message);
                _logger.LogInfo(ex.StackTrace);
                Thread.Sleep(2000);
                await ConnectScaleStationModuleFromController();
            }
        }

        private async Task MachineJobProcess()
        {
            while(stream.CanRead && stream.CanWrite)
            {
                var machines = await _machineRepository.GetPendingMachine();

                if (machines == null || machines.Count == 0)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                machines = machines.Where(x => x.Code == "1" || x.Code == "2").ToList();

                foreach (var machine in machines)
                {
                    try
                    {
                        if (machine.StartStatus == "PENDING" && !string.IsNullOrEmpty(machine.CurrentDeliveryCode))
                        {
                            _logger.LogInfo($"Start machine: {machine.Code}");
                            // 2. send 1
                            byte[] data = encoding.GetBytes($"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[!]");
                            stream.Write(data, 0, data.Length);

                            // 3. receive 1
                            data = new byte[BUFFER_SIZE];
                            stream.Read(data, 0, BUFFER_SIZE);

                            var response = encoding.GetString(data).Trim();

                            if (response == null || response.Length == 0)
                            {
                                _logger.LogInfo($"Khong co du lieu tra ve");
                                Thread.Sleep(2000);
                                continue;
                            }
                            _logger.LogInfo($"Du lieu tra ve: {response}");

                            if (response.Contains($"*[Start][MDB][{machine.Code}]#OK##{machine.CurrentDeliveryCode}[!]"))
                            {
                                machine.StartStatus = "ON";
                                machine.StopStatus = "OFF";
                                await _machineRepository.UpdateMachine(machine);
                                _logger.LogInfo($"Start machine {machine.Code} thanh cong!");
                            }
                            else
                            {
                                _logger.LogInfo($"Tin hieu phan hoi khong thanh cong");
                                Thread.Sleep(2000);
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
                                Thread.Sleep(2000);
                                continue;
                            }
                            _logger.LogInfo($"Du lieu tra ve: {response}");

                            if (response.Contains($"*[Stop][MDB][{machine.Code}]#OK##{machine.CurrentDeliveryCode}[!]"))
                            {
                                machine.StartStatus = "OFF";
                                machine.StopStatus = "ON";
                                await _machineRepository.UpdateMachine(machine);
                                _logger.LogInfo($"Stop machine {machine.Code} thanh cong!");
                            }
                            else
                            {
                                _logger.LogInfo($"Tin hieu phan hoi khong thanh cong");
                                Thread.Sleep(2000);
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInfo($"Khong the xu ly {machine.Code}");
                        _logger.LogError($"{ex.Message}");
                        _logger.LogError($"{ex.StackTrace}");
                    }
                }
            }
            await ConnectScaleStationModuleFromController();
        }
    }
}
