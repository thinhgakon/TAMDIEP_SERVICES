using System;
using System.Threading.Tasks;
using Quartz;
using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR.Client;
using System.ServiceProcess;
using XHTD_SERVICES_TRACE.Models.Request;

namespace XHTD_SERVICES_TRACE.Jobs
{
    public class TraceServiceJob : IJob
    {
        private string ServiceName { get; set; }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;
            ServiceName = dataMap.GetString("ServiceName");

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            await Task.Run(async () =>
            {
                await Trace();
            });
        }

        public async Task Trace()
        {
            try
            {
                var connection = Program.HubConnection;
                try
                {
                    using (ServiceController service = new ServiceController(ServiceName))
                    {
                        await connection.SendAsync("Trace", new SystemTraceDto()
                        {
                            Code = service.ServiceName,
                            MachineName = Environment.MachineName,
                            Status = service.Status == ServiceControllerStatus.Running,
                            Log = service.Status.ToString()
                        });
                    }
                }
                catch (Exception ex)
                {
                    await connection.SendAsync("Trace", new SystemTraceDto()
                    {
                        Code = ServiceName,
                        MachineName = Environment.MachineName,
                        Status = null,
                        Log = ex.Message
                    });
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
