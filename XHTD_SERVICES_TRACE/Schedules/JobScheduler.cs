using Quartz.Impl;
using Quartz;
using log4net;
using XHTD_SERVICES_TRACE.Jobs;
using System.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.ServiceProcess;
using System.Linq;
using XHTD_SERVICES_TRACE.Models.Request;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System;
using System.Threading;

namespace XHTD_SERVICES_TRACE.Schedules
{
    public class JobScheduler
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IScheduler _scheduler;
        private static List<SystemTraceServiceInfoDto> data = new List<SystemTraceServiceInfoDto>();
        public JobScheduler(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async void Start()
        {
            await GetJob();
            await _scheduler.Start();

            while (data.Count == 0)
            {
            }

            foreach (var item in data)
            {
                IJobDetail detail = JobBuilder.Create<TraceServiceJob>()
                    .WithIdentity($"{item.Code}_Job")
                    .SetJobData(new JobDataMap() { new KeyValuePair<string, object>("ServiceName", item.Code) })
                    .Build();

                ITrigger trigger = TriggerBuilder.Create()
                     .StartNow()
                     .WithIdentity($"{item.Code}_Trigger")
                     .WithSimpleSchedule(x => x
                          .WithIntervalInMinutes(item.Interval)
                         .RepeatForever())
                    .Build();
                await _scheduler.ScheduleJob(detail, trigger);
            }
        }

        public async Task GetJob()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(Program.SignalRUrl)
                .Build();

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            string IPAddress = Environment.MachineName;
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    var ipProps = networkInterface.GetIPProperties();
                    var ipAddresses = ipProps.UnicastAddresses;
                    IPAddress += " | " + ipAddresses.FirstOrDefault(ipAddress => ipAddress.Address.AddressFamily == AddressFamily.InterNetwork)?.Address?.ToString();
                }
            }

            var services = ServiceController.GetServices().Where(x => x.ServiceName.StartsWith("XHTD")).ToList();

            connection.StartAsync().Wait();
            await connection.SendAsync("SyncJob", services.Select(x => new SystemTraceSyncDto()
            {
                Code = x.ServiceName,
                Status = x.Status == ServiceControllerStatus.Running,
                Address = IPAddress
            }));

            connection.On<List<SystemTraceServiceInfoDto>>("SyncJob", (message) =>
            {
                data = message;
            });

            connection.On<SystemTraceStartStopDto>("Start", async (model) =>
            {
                try
                {
                    var serviceName = model.Code;
                    if (model.MachineName.Trim() != Environment.MachineName.Trim())
                    {
                        return;
                    }
                    if (string.IsNullOrEmpty(serviceName))
                    {
                        return;
                    }
                    using (ServiceController service = new ServiceController(serviceName))
                    {
                        if (service.Status == ServiceControllerStatus.Stopped)
                        {
                            service.Start();
                            await connection.SendAsync("Trace", new SystemTraceDto()
                            {
                                Code = service.ServiceName,
                                MachineName = Environment.MachineName,
                                Status = true,
                                Log = service.Status.ToString()
                            });
                        }


                    }
                }
                catch (Exception ex)
                {
                    await connection.SendAsync("Trace", new SystemTraceDto()
                    {
                        Code = model.Code,
                        MachineName = Environment.MachineName,
                        Status = null,
                        Log = ex.Message
                    });
                }

            });

            connection.On<SystemTraceStartStopDto>("Stop", async (model) =>
            {
                try
                {
                    var serviceName = model.Code;
                    if (string.IsNullOrEmpty(serviceName))
                    {
                        return;
                    }
                    if (model.MachineName.Trim() != Environment.MachineName.Trim())
                    {
                        return;
                    }
                    using (ServiceController service = new ServiceController(serviceName))
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            await connection.SendAsync("Trace", new SystemTraceDto()
                            {
                                Code = service.ServiceName,
                                MachineName = Environment.MachineName,
                                Status = false,
                                Log = service.Status.ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    await connection.SendAsync("Trace", new SystemTraceDto()
                    {
                        Code = model.Code,
                        MachineName = Environment.MachineName,
                        Status = null,
                        Log = ex.Message
                    });
                }

            });

            connection.On<SystemTraceServiceInfoDto>("Update", (message) =>
            {
                TriggerKey oldTriggerKey = new TriggerKey($"{message.Code}_Trigger");
                _scheduler.UnscheduleJob(oldTriggerKey);

                ITrigger trigger = TriggerBuilder.Create()
                      .StartNow()
                      .WithSimpleSchedule(x => x
                           .WithIntervalInMinutes(message.Interval)
                          .RepeatForever())
                     .Build();
                _scheduler.ScheduleJob(trigger);
            });
        }
    }
}
