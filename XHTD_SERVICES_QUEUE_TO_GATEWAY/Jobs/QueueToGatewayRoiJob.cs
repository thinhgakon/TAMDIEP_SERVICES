using Quartz;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using log4net;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs
{
    public class QueueToGatewayRoiJob : IJob
    {
        ILog _logger = LogManager.GetLogger("RoiFileAppender");

        private const string TYPE_PRODUCT = "ROI";

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public QueueToGatewayRoiJob(StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                WriteLogInfo($"Start Queue To Gateway: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");

                await PushToDbCallProccesss();
            });
        }

        public async Task PushToDbCallProccesss()
        {
            try
            {
                var limitVehicle = 5;
                var isCall = true;

                using (var db = new XHTD_Entities())
                {
                    var isCallConfig = await db.tblSystemParameters.FirstOrDefaultAsync(x => x.Code == $"IS_CALL_{TYPE_PRODUCT}");
                    isCall = isCallConfig.Value == "1" ? true : false;

                    var maxVehicleConfig = await db.tblSystemParameters.FirstOrDefaultAsync(x => x.Code == $"MAX_VEHICLE_{TYPE_PRODUCT}");
                    limitVehicle = int.Parse(maxVehicleConfig.Value);
                }

                if (!isCall)
                {
                    WriteLogInfo($"Cấu hình gọi xe {TYPE_PRODUCT} đang tắt => Kết thúc");
                    return;
                }

                WriteLogInfo($"Số xe {TYPE_PRODUCT} tối đa: {limitVehicle}");
                ProcessPushToDBCall(limitVehicle);
            }
            catch (Exception ex)
            {
                WriteLogInfo(ex.Message);
            }
        }
        public void ProcessPushToDBCall(int LimitVehicle)
        {
            try
            {
                //get sl xe trong bãi chờ máng ứng với sp
                var vehicleFrontYard = _storeOrderOperatingRepository.CountStoreOrderWaitingIntoTroughByType(TYPE_PRODUCT);

                WriteLogInfo($"Số xe {TYPE_PRODUCT} đang trong bãi chờ: {vehicleFrontYard}");

                if (vehicleFrontYard < LimitVehicle)
                {
                    ProcessUpdateStepIntoYard(LimitVehicle - vehicleFrontYard);
                }

                else
                {
                    WriteLogInfo($"Số xe {TYPE_PRODUCT} trong nhà máy đã đạt tối đa => Kết thúc");
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($@"Có lỗi xảy ra khi thêm xe vào hàng đợi gọi loa: {ex.Message}");
            }
        }
        public void ProcessUpdateStepIntoYard(int topX)
        {
            try
            {
                using (var db = new XHTD_Entities())
                {
                    var orders = db.tblStoreOrderOperatings.Where(x => x.Step == 10 && x.TypeProduct.Equals(TYPE_PRODUCT) && x.IndexOrder2 == 0 && (x.DriverUserName ?? "") != "").OrderBy(x => x.IndexOrder).Take(topX).ToList();
                    foreach (var order in orders)
                    {
                        var dateTimeCall = DateTime.Now.AddMinutes(-2);
                        if (order.TimeConfirm1 > dateTimeCall) continue;
                        var sqlUpdate = "UPDATE tblStoreOrderOperating SET Step =  4,  TimeConfirm4 = ISNULL(TimeConfirm4, GETDATE()), LogProcessOrder = CONCAT(LogProcessOrder, N'#Đưa vào hàng đợi mời xe vào lúc ', FORMAT(getdate(), 'dd/MM/yyyy HH:mm:ss')) WHERE OrderId = @OrderId AND ISNULL(Step,0) <> 4";
                        var updateResponse = db.Database.ExecuteSqlCommand(sqlUpdate, new SqlParameter("@OrderId", order.OrderId));
                        if (updateResponse > 0)
                        {
                            // xử lý nghiệp vụ đẩy vào db để xử lý gọi loa
                            var tblCallVehicleStatusDb = db.tblCallVehicleStatus.FirstOrDefault(x => x.StoreOrderOperatingId == order.Id && x.IsDone == false);
                            if (tblCallVehicleStatusDb != null) continue;
                            var logString = $@"Xe được mời vào lúc {DateTime.Now} .";
                            var newTblVehicleStatus = new tblCallVehicleStatu
                            {
                                StoreOrderOperatingId = order.Id,
                                CountTry = 0,
                                TypeProduct = order.TypeProduct,
                                CreatedOn = DateTime.Now,
                                ModifiledOn = DateTime.Now,
                                LogCall = logString,
                                IsDone = false
                            };
                            db.tblCallVehicleStatus.Add(newTblVehicleStatus);
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"Có lỗi xảy ra khi cập nhật trạng thái đơn hàng: " + ex.Message);
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
