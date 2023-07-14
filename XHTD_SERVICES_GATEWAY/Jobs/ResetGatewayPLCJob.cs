using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_GATEWAY.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;
using Microsoft.AspNet.SignalR.Client;
using System.Threading;
using XHTD_SERVICES.Data.Common;
using Autofac;
using XHTD_SERVICES_GATEWAY.Business;
using XHTD_SERVICES_GATEWAY.Hubs;
using XHTD_SERVICES_GATEWAY.Devices;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class ResetGatewayPLCJob : IJob
    {
        protected readonly PLCBarrier _barrier;

        protected readonly GatewayLogger _gatewayLogger;

        public ResetGatewayPLCJob(
            PLCBarrier barrier,
            GatewayLogger gatewayLogger
            )
        {
            _barrier = barrier;
            _gatewayLogger = gatewayLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                ResetPLC();
            });                                                                                                                     
        }

        private void ResetPLC()
        {
            DIBootstrapper.Init().Resolve<BarrierControl>().ResetAllOutputPorts();
        }
    }
}
