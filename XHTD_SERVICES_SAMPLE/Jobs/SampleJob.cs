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

namespace XHTD_SERVICES_SAMPLE.Jobs
{
    public class SampleJob : IJob
    {
        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly Notification _notification;

        protected readonly SampleLogger _sampleLogger;

        const int MAX_ORDER_IN_QUEUE_TO_CALL = 2;

        public SampleJob(
            CategoriesDevicesRepository categoriesDevicesRepository,
            Notification notification,
            SampleLogger callInTroughLogger
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

            _notification.SendNotification("", null, 0, 0, null, 0, null, null, null);
        }
    }
}
