using Quartz;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_SYNC_ORDER.Jobs
{
    public class SyncUpdateLotNumberJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;
        protected readonly Notification _notification;
        protected readonly SyncOrderLogger _syncOrderLogger;

        public SyncUpdateLotNumberJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            Notification notification,
            SyncOrderLogger syncOrderLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
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
                await UpdateLotNumberProcess();
            });
        }

        public async Task UpdateLotNumberProcess()
        {
            _syncOrderLogger.LogInfo($"Start Update Lot Number: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");

            try
            {
                using (var dbContext = new XHTD_Entities())
                {
                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => (x.Step == (int)OrderStep.DA_CAN_RA ||
                                                             x.Step == (int)OrderStep.DA_HOAN_THANH ||
                                                             x.Step == (int)OrderStep.DA_GIAO_HANG) &&
                                                             string.IsNullOrEmpty(x.LotNumber))
                                                .ToListAsync();

                    foreach (var order in orders)
                    {
                        await _storeOrderOperatingRepository.UpdateLotNumber(order.DeliveryCode);

                        _syncOrderLogger.LogInfo($"Cập nhật số lô cho đơn {order.DeliveryCode} - {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                    }
                }
            }
            catch (Exception ex)
            {
                _syncOrderLogger.LogInfo($"Start Update Lot Number ERROR: {ex.Message} - {ex.InnerException}");
                return;
            }
        }
    }
}
