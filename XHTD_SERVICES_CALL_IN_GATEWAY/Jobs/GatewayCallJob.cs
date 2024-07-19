using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES_CALL_IN_GATEWAY.Jobs
{
    public class GatewayCallJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly GatewayCallLogger _gatewayLogger;

        public GatewayCallJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            GatewayCallLogger gatewayCallLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _gatewayLogger = gatewayCallLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                GatewayCallProcess();
            });
        }

        private void GatewayCallProcess()
        {
            _gatewayLogger.LogInfo("Service is running...");
        }
    }
}
