using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_AUTO_REINDEX.Jobs
{
    public class AutoReindexJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly AutoReindexLogger _autoReindexLogger;

        public AutoReindexJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            AutoReindexLogger autoReindexLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
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
                await AutoReindexProcess();
            });
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

            var orderXBaoNoIndexs = await _storeOrderOperatingRepository.GetOrdersXiMangBaoNoIndex();
            if (orderXBaoNoIndexs != null && orderXBaoNoIndexs.Count > 0)
            {
                foreach (var orderXBaoNoIndex in orderXBaoNoIndexs)
                {
                    var maxIndex = _storeOrderOperatingRepository.GetMaxIndexByCatId("XI_MANG_BAO");
                    await _storeOrderOperatingRepository.UpdateIndex(orderXBaoNoIndex.Id, maxIndex + 1);
                }
            }
        }
    }
}
