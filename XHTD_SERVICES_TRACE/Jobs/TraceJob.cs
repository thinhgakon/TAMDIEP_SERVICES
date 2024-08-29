using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_TRACE.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES_TRACE.Models.Values;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;
using System.ServiceProcess;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

namespace XHTD_SERVICES_TRACE.Jobs
{
    public class TraceJob : IJob
    {
        protected readonly TraceLogger _logger;
        private readonly string IPAddress;

        public TraceJob(TraceLogger logeer)
        {
            _logger = logeer;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            IPAddress = string.Empty;
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    var ipProps = networkInterface.GetIPProperties();
                    var ipAddresses = ipProps.UnicastAddresses;
                    IPAddress = ipAddresses.FirstOrDefault(ipAddress => ipAddress.Address.AddressFamily == AddressFamily.InterNetwork)?.Address?.ToString();
                }
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {

            });
        }

        public void UpdateStatus()
        {
          
        }
    }
}
