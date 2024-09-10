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
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs
{
    public class QueueToGatewayOtherJob : IJob
    {
        ILog _logger = LogManager.GetLogger("OtherFileAppender");

        private const string TYPE_PRODUCT = "OTHER";

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public QueueToGatewayOtherJob(StoreOrderOperatingRepository storeOrderOperatingRepository)
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
                WriteLogInfo($"====================== Bắt đầu job {TYPE_PRODUCT} ======================");

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
                    WriteLogInfo($"Cấu hình gọi xe đang tắt => Kết thúc");
                    return;
                }

                WriteLogInfo($"1. Số xe cấu hình tối đa: {limitVehicle}");

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

                WriteLogInfo($"2. Số xe đang chờ trong nhà máy: {vehicleFrontYard}");

                if (vehicleFrontYard < LimitVehicle)
                {
                    ProcessUpdateStepIntoYard(LimitVehicle - vehicleFrontYard);
                }
                else
                {
                    WriteLogInfo($"3. Số xe trong nhà máy đã đạt tối đa => Kết thúc");
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($@"ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException} => Kết thúc");
            }
        }

        public void ProcessUpdateStepIntoYard(int topX)
        {
            WriteLogInfo($"3. Tìm và thêm {topX} xe đưa vào hàng đợi gọi loa");

            try
            {
                using (var db = new XHTD_Entities())
                {
                    var orders = db.tblStoreOrderOperatings.Where(x => x.Step == (int)OrderStep.DA_XAC_THUC
                                                                    && x.TypeProduct.Equals(TYPE_PRODUCT)
                                                                    && x.IndexOrder2 == 0 && (x.DriverUserName ?? "") != "")
                                                            .OrderBy(x => x.IndexOrder)
                                                            .Take(topX)
                                                            .ToList();

                    if (orders == null || orders.Count == 0)
                    {
                        WriteLogInfo($"4. Không tìm thấy xe {TYPE_PRODUCT} nào => Kết thúc");
                        return;
                    }
                    else
                    {
                        WriteLogInfo($"4. Các xe phù hợp: {string.Join(", ", orders.Select(order => order.Vehicle))}");
                    }

                    foreach (var order in orders)
                    {
                        WriteLogInfo($"4.1. Tiến hành thêm xe vào hàng đợi: {order.DeliveryCode} --- {order.Vehicle}");

                        //var dateTimeCall = DateTime.Now.AddSeconds(-15);
                        //if (order.TimeConfirm1 > dateTimeCall) continue;

                        var sqlUpdate = $@"UPDATE tblStoreOrderOperating 
                                           SET Step = {(int)OrderStep.CHO_GOI_XE}, 
                                               TimeConfirm11 = ISNULL(TimeConfirm11, GETDATE()), 
                                               LogProcessOrder = CONCAT(LogProcessOrder, N'#Đưa vào hàng đợi mời xe vào lúc ', FORMAT(getdate(), 'dd/MM/yyyy HH:mm:ss')) 
                                           WHERE OrderId = @OrderId 
                                                 AND ISNULL(Step, 0) <> {(int)OrderStep.CHO_GOI_XE}
                                           ";

                        var updateResponse = db.Database.ExecuteSqlCommand(sqlUpdate, new SqlParameter("@OrderId", order.OrderId));
                        if (updateResponse > 0)
                        {
                            // xử lý nghiệp vụ đẩy vào db để xử lý gọi loa
                            var tblCallVehicleStatusDb = db.tblCallVehicleStatus.FirstOrDefault(x => x.StoreOrderOperatingId == order.Id && x.IsDone == false);
                            if (tblCallVehicleStatusDb != null) continue;
                            var logString = $@"Đưa xe vào hàng đợi gọi loa lúc {DateTime.Now}. ";
                            var newTblVehicleStatus = new tblCallVehicleStatu
                            {
                                StoreOrderOperatingId = order.Id,
                                CountTry = 0,
                                TypeProduct = order.TypeProduct,
                                CreatedOn = DateTime.Now,
                                ModifiledOn = DateTime.Now,
                                LogCall = logString,
                                IsDone = false,
                                CallType = CallType.CONG
                            };
                            db.tblCallVehicleStatus.Add(newTblVehicleStatus);
                            db.SaveChanges();

                            WriteLogInfo($"4.2. Thêm thành công");
                        }
                        else
                        {
                            WriteLogInfo($"4.2. Thêm thất bại");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"ERROR ProcessUpdateStepIntoYard: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException} => Kết thúc");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
