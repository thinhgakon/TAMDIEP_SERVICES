using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs
{
    public class QueueToGatewayExportPlanPcb30Job : IJob
    {
        ILog _logger = LogManager.GetLogger("Pcb30FileAppender");

        private const string TYPE_PRODUCT = "PCB30";

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public QueueToGatewayExportPlanPcb30Job(StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            await Task.Run(() =>
            {
                WriteLogInfo($"--------------- START JOB ---------------");
                QueueToCallProccess();
            });
        }

        public void QueueToCallProccess()
        {
            try
            {
                var IsCall = true;
                using (var db = new XHTD_Entities())
                {
                    var isCallConfig = db.tblSystemParameters.FirstOrDefault(x => x.Code == $"IS_CALL_{TYPE_PRODUCT}");
                    IsCall = isCallConfig.Value == "1" ? true : false;
                }

                if (!IsCall)
                {
                    WriteLogInfo($@"Cấu hình gọi loa đang OFF. Kiểm tra IsCall trong tblSystemParameters");
                    return;
                }

                using (var db = new XHTD_Entities())
                {
                    var callConfigs = db.tblCallToGatewayConfigs.Where(x => x.Status == 1).ToList();

                    foreach (var itemConfig in callConfigs)
                    {
                        int sourceDocumentId = (int)itemConfig.SourceDocumentId;
                        int maxVehicleConfig = (int)itemConfig.MaxVehiclePcb30;
                        ProcessByExportPlan(maxVehicleConfig, sourceDocumentId);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo(ex.Message);
            }
        }

        public void ProcessByExportPlan(int LimitVehicle, int sourceDocumentId)
        {
            WriteLogInfo($@" ======= Xử lý kế hoạch: sourceDocumentId={sourceDocumentId}, cấu hình xe tối đa {LimitVehicle} =======");
            try
            {
                //get sl xe trong bãi chờ máng ứng với sp
                var orders = _storeOrderOperatingRepository.CountStoreOrderWaitingIntoTroughByTypeAndExportPlan(TYPE_PRODUCT, sourceDocumentId);
                WriteLogInfo($@"1. Các đơn {TYPE_PRODUCT} đang trong nhà máy hiện tại: {string.Join(",", orders.Select(x => x.DeliveryCode))}");

                var vehicleFrontYard = orders?.Select(x => x.Vehicle).Distinct().ToList().Count ?? 0;

                WriteLogInfo($@"1. Số xe chờ lấy hàng: {vehicleFrontYard}");

                if (vehicleFrontYard >= LimitVehicle)
                {
                    WriteLogInfo($@"1.1. Số xe đang chờ vượt quá số xe tối đa => Kết thúc");
                    return;
                }

                AddVehicleIntoQueue(LimitVehicle - vehicleFrontYard, sourceDocumentId);
            }
            catch (Exception ex)
            {
                WriteLogInfo($@"ProcessByExportPlan {TYPE_PRODUCT}: {ex.Message}");
            }
        }

        public void AddVehicleIntoQueue(int topX, int? sourceDocumentId)
        {
            try
            {
                WriteLogInfo($@"2. Tìm và thêm {topX} xe vào hàng đợi");

                using (var db = new XHTD_Entities())
                {
                    var callConfigs = db.tblCallToGatewayConfigs.Where(x => x.Status == 1 && x.SourceDocumentId != 0).ToList();
                    var sourceDocumentIds = callConfigs.Select(x => x.SourceDocumentId).ToList();

                    var query = db.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_XAC_THUC &&
                                                x.TypeProduct.Equals(TYPE_PRODUCT) &&
                                                x.IsVoiced == false);

                    if (sourceDocumentId != 0)
                    {
                        query = query.Where(x => x.SourceDocumentId == sourceDocumentId);
                    }
                    else
                    {
                        query = query.Where(x => x.SourceDocumentId == null ||
                                                 x.SourceDocumentId == 0 ||
                                                (x.SourceDocumentId != null && !sourceDocumentIds.Contains(x.SourceDocumentId)));
                    }

                    var orders = query.OrderBy(x => x.IndexOrder)
                                      .ThenBy(x => x.TimeConfirm10)
                                      .Take(topX)
                                      .ToList();

                    if (orders == null || orders.Count == 0)
                    {
                        WriteLogInfo($"3. Không tìm thấy xe {TYPE_PRODUCT} nào với số hiệu hợp đồng: {sourceDocumentId} => Kết thúc");
                        return;
                    }
                    else
                    {
                        WriteLogInfo($"3. Có {orders.Count} xe phù hợp: {string.Join(", ", orders.Select(order => order.Vehicle))}");
                    }

                    foreach (var order in orders)
                    {
                        WriteLogInfo($"4. Tiến hành thêm xe vào hàng đợi: {order.DeliveryCode} --- {order.Vehicle}");

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
                            var tblCallVehicleStatusDb = db.tblCallVehicleStatus
                                                        .FirstOrDefault(x => x.StoreOrderOperatingId == order.Id
                                                                        && x.IsDone == false
                                                                        && x.CallType == CallType.CONG);

                            if (tblCallVehicleStatusDb != null)
                            {
                                WriteLogInfo($"4.1. Đã tồn tại bản ghi chờ gọi loa => Không thêm xe này");
                                continue;
                            }

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

                            WriteLogInfo($"4.1. Thêm thành công");
                        }
                        else
                        {
                            WriteLogInfo($"4.1. Thêm thất bại");
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
