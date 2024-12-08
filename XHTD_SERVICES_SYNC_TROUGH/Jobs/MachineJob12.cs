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
using log4net;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class MachineJob12 : IJob, IDisposable
    {
        ILog _logger = LogManager.GetLogger("Machine12FileAppender");

        private readonly MachineRepository _machineRepository;
        protected readonly Notification _notification;

        static SimpleTcpClient client;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string MachineResponse = string.Empty;

        private const string IP_ADDRESS = "192.168.13.189";
        private const int PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        private const string MACHINE_1_CODE = "1";
        private const string MACHINE_2_CODE = "2";

        public MachineJob12(MachineRepository machineRepository, Notification notification)
        {
            _machineRepository = machineRepository;
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
                    WriteLogInfo($"Machine Job Ket noi thanh cong MDB {MACHINE_1_CODE}|{MACHINE_2_CODE} --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    await MachineJobProcess(machines);
                }
                else
                {
                    WriteLogInfo($"Machine Job Ket noi that bai MDB {MACHINE_1_CODE}|{MACHINE_2_CODE} --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }

                if (client != null)
                {
                    client.Dispose();
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"Machine Job ERROR IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}: {ex.Message} -- {ex.StackTrace}");
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
                        WriteLogInfo($"Start machine code: {machine.Code} - msgh: {machine.CurrentDeliveryCode}============================================");

                        var command = (machine.StartCountingFrom == null || machine.StartCountingFrom == 0) ?
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[!]" :
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[N]{machine.StartCountingFrom}[!]";

                        WriteLogInfo($"1. Gửi lệnh: {command}");
                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            WriteLogInfo($"2. Không có phản hồi");
                            continue;
                        }
                        WriteLogInfo($"2. Phản hồi: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Start][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "ON";
                            machine.StopStatus = "OFF";

                            await _machineRepository.UpdateMachine(machine);

                            WriteLogInfo($"2.1. Start thành công");

                            string troughCode = string.Empty;
                            string vehicle = string.Empty;
                            string bookQuantity = string.Empty;
                            string locationCodeTgc = string.Empty;

                            using (var db = new XHTD_Entities())
                            {
                                var callToTrough = await db.tblCallToTroughs.FirstOrDefaultAsync(x => x.DeliveryCode == machine.CurrentDeliveryCode && x.IsDone == false);
                                if (callToTrough != null)
                                {
                                    var trough = await db.tblTroughs.FirstOrDefaultAsync(x => x.Code == callToTrough.Machine);
                                    if (trough != null)
                                    {
                                        trough.DeliveryCodeCurrent = machine.CurrentDeliveryCode;
                                        troughCode = trough.Code;
                                    }
                                }

                                var currentOrder = await db.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.DeliveryCode == machine.CurrentDeliveryCode);
                                if (currentOrder != null)
                                {
                                    vehicle = currentOrder.Vehicle;
                                    bookQuantity = currentOrder.SumNumber.ToString();
                                    locationCodeTgc = currentOrder.LocationCodeTgc;
                                    currentOrder.StartPrintData = DateTime.Now;
                                    currentOrder.PrintMachineCode = machine.Code;
                                    currentOrder.PrintTroughCode = troughCode;

                                    SendNotificationAPI(string.Empty, machine.Code, machine.StartStatus, machine.StopStatus);
                                    SendMachineStartNotification(machine.Code, troughCode, machine.CurrentDeliveryCode, vehicle, bookQuantity, locationCodeTgc);
                                }

                                await db.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            WriteLogInfo($"2.1. Start thất bại");
                            continue;
                        }
                    }

                    if (machine.StopStatus == "PENDING")
                    {
                        var currentDeliveryCode = machine.CurrentDeliveryCode;

                        WriteLogInfo($"Stop machine code: {machine.Code} -- msgh: {currentDeliveryCode} ============================================");

                        var command = $"*[Stop][MDB][{machine.Code}][!]";

                        WriteLogInfo($"1. Gửi lệnh: {command}");
                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            WriteLogInfo($"2. Không có phản hồi");
                            continue;
                        }
                        WriteLogInfo($"2. Phản hồi: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Stop][MDB][{machine.Code}]#OK#"))
                        {
                            using (var db = new XHTD_Entities())
                            {
                                var currentOrder = await db.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.DeliveryCode == currentDeliveryCode);
                                var isFromWeightOut = currentOrder?.Step == (int)OrderStep.DA_CAN_RA ? true : false;

                                if (currentOrder != null)
                                {
                                    currentOrder.StopPrintData = DateTime.Now;
                                    currentOrder.IsFromWeightOut = isFromWeightOut;
                                    await db.SaveChangesAsync();
                                }

                                SendNotificationAPI(string.Empty, machine.Code, machine.StartStatus, machine.StopStatus);
                                SendMachineStopNotification(machine.Code, string.Empty, currentDeliveryCode, string.Empty, isFromWeightOut);
                            }

                            machine.StartStatus = "OFF";
                            machine.StopStatus = "ON";
                            machine.CurrentDeliveryCode = null;
                            machine.StartCountingFrom = 0;

                            await _machineRepository.UpdateMachine(machine);

                            WriteLogInfo($"2.1. Stop thành công");
                        }
                        else
                        {
                            WriteLogInfo($"2.1. Stop thất bại");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogInfo($"MachineJobProcess ERROR: Code={machine.Code} --- {ex.Message} --- {ex.StackTrace}");
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
                WriteLogInfo($"SendNotificationAPI Machine {MACHINE_1_CODE}|{MACHINE_2_CODE} Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendMachineStartNotification(string machineCode, string troughCode, string deliveryCode, string vehicle, string bookQuantity, string locationCodeTgc)
        {
            try
            {
                _notification.SendTroughStartData(machineCode, troughCode, deliveryCode, vehicle, bookQuantity, locationCodeTgc);
                WriteLogInfo($"Gửi signalR thành công: Machine: {machineCode} - Trough: {troughCode} - DeliveryCode: {deliveryCode} - Vehicle: {vehicle} - BookQuantity: {bookQuantity} - LocationCodeTgc: {locationCodeTgc}");
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SendMachineStartNotification Machine {MACHINE_1_CODE}|{MACHINE_2_CODE} Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendMachineStopNotification(string machineCode, string troughCode, string deliveryCode, string vehicle, bool isFromWeightOut)
        {
            try
            {
                _notification.SendTroughStopData(machineCode, troughCode, deliveryCode, vehicle, isFromWeightOut);
                WriteLogInfo($"Gửi signalR thành công: Machine: {machineCode} - Trough: {troughCode} - DeliveryCode: {deliveryCode} - Vehicle: {vehicle} - IsFromWeightOut: {isFromWeightOut}");
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SendMachineStopNotification Machine {MACHINE_1_CODE}|{MACHINE_2_CODE} Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
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
                WriteLogInfo($"MachineJob {MACHINE_1_CODE}|{MACHINE_2_CODE}: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
