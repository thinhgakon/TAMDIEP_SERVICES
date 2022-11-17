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

namespace XHTD_SERVICES_QUEUE_TO_CALL.Jobs
{
    public class QueueToCallJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly Notification _notification;

        protected readonly QueueToCallLogger _syncOrderLogger;

        private static string strToken;

        public QueueToCallJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            Notification notification,
            QueueToCallLogger syncOrderLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _notification = notification;
            _syncOrderLogger = syncOrderLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await SyncOrderProcess();
            });
        }

        public async Task SyncOrderProcess()
        {
            _syncOrderLogger.LogInfo("start process QueueToCallJob");
        }
    }
}
