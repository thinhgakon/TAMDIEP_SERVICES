using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using XHTD_SERVICES.Data.Models.Values;
using System.Threading;

namespace XHTD_SERVICES_COMPLETE_ORDER.Jobs
{
    public class CompleteOrderJob : IJob
    {
        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly Notification _notification;

        protected readonly CompleteOrderLogger _sampleLogger;

        const int MAX_ORDER_IN_QUEUE_TO_CALL = 2;

        public CompleteOrderJob(
            CategoriesDevicesRepository categoriesDevicesRepository,
            Notification notification,
            CompleteOrderLogger callInTroughLogger
            )
        {
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _notification = notification;
            _sampleLogger = callInTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                SampleProcess();
            });
        }

        public async void SampleProcess()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("BV");

            _sampleLogger.LogInfo("start process SampleJob");

            _notification.SendNotification("GETWAY", null, null, "123456", null, "Không xác định đơn hàng hợp lệ");
        }
    }
}
