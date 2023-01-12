using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_AUTO_REINDEX.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES_AUTO_REINDEX.Models.Values;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES_AUTO_REINDEX.Jobs
{
    public class AutoReindexJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Notification _notification;

        protected readonly AutoReindexLogger _autoReindexLogger;

        protected const string SYNC_ORDER_ACTIVE = "SYNC_ORDER_ACTIVE";

        protected const string SYNC_ORDER_HOURS = "SYNC_ORDER_HOURS";

        private static bool isActiveService = true;

        private static int numberHoursSearchOrder = 48;

        public AutoReindexJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            SystemParameterRepository systemParameterRepository,
            Notification notification,
            AutoReindexLogger autoReindexLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _systemParameterRepository = systemParameterRepository;
            _notification = notification;
            _autoReindexLogger = autoReindexLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                // Get System Parameters
                await LoadSystemParameters();

                if (!isActiveService)
                {
                    _autoReindexLogger.LogInfo("Service dong bo don hang dang TAT.");
                    return;
                }

                await AutoReindexProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SYNC_ORDER_ACTIVE);
            var numberHoursParameter = parameters.FirstOrDefault(x => x.Code == SYNC_ORDER_HOURS);

            if(activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }

            if (numberHoursParameter != null)
            {
                numberHoursSearchOrder = Convert.ToInt32(numberHoursParameter.Value);
            }
        }

        public async Task AutoReindexProcess()
        {
            _autoReindexLogger.LogInfo("Start process AutoReindexProcess");

            //1.  Xep lot XI_MANG_XA
            var orderRoiIndexds = await _storeOrderOperatingRepository.GetOrdersXiMangRoiIndexd();
            if (orderRoiIndexds != null && orderRoiIndexds.Count > 0)
            {
                int i = 1;
                foreach (var orderRoiIndexd in orderRoiIndexds)
                {
                    await _storeOrderOperatingRepository.UpdateIndex(orderRoiIndexd.Id, i);
                    i++;
                }
            }

            var orderXRoiNoIndexs = await _storeOrderOperatingRepository.GetOrdersXiMangRoiNoIndex();
            if (orderXRoiNoIndexs != null && orderXRoiNoIndexs.Count > 0)
            {
                foreach (var orderXRoiNoIndex in orderXRoiNoIndexs)
                {
                    var maxIndex = _storeOrderOperatingRepository.GetMaxIndexByCatId("XI_MANG_XA");
                    await _storeOrderOperatingRepository.UpdateIndex(orderXRoiNoIndex.Id, maxIndex + 1);
                }
            }


            //2. Xep lot XI_MANG_BAO
            var orderBaoIndexds = await _storeOrderOperatingRepository.GetOrdersXiMangBaoIndexd();
            if (orderBaoIndexds != null && orderBaoIndexds.Count > 0)
            {
                int j = 1;
                foreach (var orderBaoIndexd in orderBaoIndexds)
                {
                    await _storeOrderOperatingRepository.UpdateIndex(orderBaoIndexd.Id, j);
                    j++;
                }
            }

            var orderXBaoNoIndexs = await _storeOrderOperatingRepository.GetOrdersXiMangRoiNoIndex();
            if (orderXBaoNoIndexs != null && orderXBaoNoIndexs.Count > 0)
            {
                foreach (var orderXBaoNoIndex in orderXBaoNoIndexs)
                {
                    var maxIndex = _storeOrderOperatingRepository.GetMaxIndexByCatId("XI_MANG_XA");
                    await _storeOrderOperatingRepository.UpdateIndex(orderXBaoNoIndex.Id, maxIndex + 1);
                }
            }
        }
    }
}
