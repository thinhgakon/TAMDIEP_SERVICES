using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;
using log4net;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES.Helper
{
    public static class OrderValidator
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool IsValidOrderEntraceGateway(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang chieu VAO: order = null");
                return false;
            }

            _logger.Info($"4.0. Kiem tra don hang chieu VAO: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            if ((order.Step == (int)OrderStep.DA_XAC_THUC
                || order.Step == (int)OrderStep.DANG_GOI_XE
                || order.Step == (int)OrderStep.CHO_GOI_XE
                )
                && (order.DriverUserName ?? "") != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsValidOrdersEntraceGateway(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                _logger.Info($"4.0. Don hang chieu VAO: order = null");
                return false;
            }

            foreach(var order in orders)
            {
                _logger.Info($"4.0. Kiem tra don hang chieu VAO: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");
            }

            var isValid = orders.Any(x => ((x.Step == (int)OrderStep.DA_XAC_THUC
                                            || x.Step == (int)OrderStep.DANG_GOI_XE
                                            || x.Step == (int)OrderStep.CHO_GOI_XE
                                            )
                                            && (x.DriverUserName ?? "") != "")
                                    );

            return isValid;
        }

        public static string CheckValidOrdersEntraceGateway(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                _logger.Info($"4.0. Don hang chieu VAO: order = null");

                return CheckValidRfidResultCode.CHUA_CO_DON;
            }

            foreach (var order in orders)
            {
                _logger.Info($"4.0. Kiem tra don hang chieu VAO: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");
            }

            var isValid = orders.Any(x => ((x.Step == (int)OrderStep.DA_XAC_THUC
                                            || x.Step == (int)OrderStep.DANG_GOI_XE
                                            || x.Step == (int)OrderStep.CHO_GOI_XE
                                            )
                                            && (x.DriverUserName ?? "") != "")
                                    );

            var isValidReceivedOrder = orders.Any(x => x.Step == (int)OrderStep.DA_NHAN_DON
                                        && (x.DriverUserName ?? "") != ""
                                    );

            var isValidHasOrder = orders.Any(x => x.Step == (int)OrderStep.CHUA_NHAN_DON
                                    );

            if (isValid)
            {
                return CheckValidRfidResultCode.HOP_LE;
            }
            else if (isValidReceivedOrder)
            {
                return CheckValidRfidResultCode.CHUA_XAC_THUC;
            }
            else if (isValidHasOrder)
            {
                return CheckValidRfidResultCode.CHUA_NHAN_DON;
            }
            else
            {
                return CheckValidRfidResultCode.CHUA_CO_DON;
            }
        }

        public static List<tblStoreOrderOperating> ValidOrdersEntraceGateway(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                return null;
            }

            var validOrders = orders.Where(x => ((x.Step == (int)OrderStep.DA_XAC_THUC
                                            || x.Step == (int)OrderStep.DANG_GOI_XE
                                            || x.Step == (int)OrderStep.CHO_GOI_XE
                                            )
                                            && (x.DriverUserName ?? "") != "")
                                    )
                                    .ToList();

            return validOrders;
        }

        public static bool IsValidOrderEntraceGatewayInCaseRequireCallVoice(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang chieu VAO: order = null");
                return false;
            }

            _logger.Info($"4.0. Kiem tra don hang chieu VAO: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            if ((order.Step == (int)OrderStep.DANG_GOI_XE
                )
                && (order.DriverUserName ?? "") != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsValidOrdersEntraceGatewayInCaseRequireCallVoice(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                _logger.Info($"4.0. Don hang chieu VAO: order = null");
                return false;
            }

            foreach (var order in orders) { 
                _logger.Info($"4.0. Kiem tra don hang chieu VAO: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");
            }

            var isValid = orders.Any(x => x.Step == (int)OrderStep.DANG_GOI_XE 
                                        && (x.DriverUserName ?? "") != ""
                                    );

            return isValid;
        }

        public static string CheckValidOrdersEntraceGatewayInCaseRequireCallVoice(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                _logger.Info($"4.0. Don hang chieu VAO: order = null");

                return CheckValidRfidResultCode.CHUA_CO_DON;
            }

            foreach (var order in orders)
            {
                _logger.Info($"4.0. Kiem tra don hang chieu VAO: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");
            }

            var isValid = orders.Any(x => x.Step == (int)OrderStep.DANG_GOI_XE
                                        && (x.DriverUserName ?? "") != ""
                                    );

            var isValidConfirm = orders.Any(x => (x.Step == (int)OrderStep.DA_XAC_THUC || x.Step == (int)OrderStep.CHO_GOI_XE) 
                                        && (x.DriverUserName ?? "") != ""
                                    );

            var isValidReceivedOrder = orders.Any(x => x.Step == (int)OrderStep.DA_NHAN_DON
                                        && (x.DriverUserName ?? "") != ""
                                    );

            var isValidHasOrder = orders.Any(x => x.Step == (int)OrderStep.CHUA_NHAN_DON
                                    );

            if (isValid)
            {
                return CheckValidRfidResultCode.HOP_LE;
            }
            else if (isValidConfirm)
            {
                return CheckValidRfidResultCode.CHUA_GOI_LOA;
            }
            else if (isValidReceivedOrder)
            {
                return CheckValidRfidResultCode.CHUA_XAC_THUC;
            }
            else if (isValidHasOrder)
            {
                return CheckValidRfidResultCode.CHUA_NHAN_DON;
            }
            else
            {
                return CheckValidRfidResultCode.CHUA_CO_DON;
            }
        }

        public static List<tblStoreOrderOperating> ValidOrdersEntraceGatewayInCaseRequireCallVoice(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                return null;
            }

            var validOrders = orders.Where(x => x.Step == (int)OrderStep.DANG_GOI_XE
                                        && (x.DriverUserName ?? "") != ""
                                    )
                                    .ToList();

            return validOrders;
        }

        public static bool IsValidOrderExitGateway(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang chieu RA: order = null");
                return false;
            }

            _logger.Info($"4.0. Kiem tra don hang chieu RA: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            if (
                order.Step == (int)OrderStep.DA_CAN_RA
                && (order.DriverUserName ?? "") != ""
               )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsValidOrdersExitGateway(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                _logger.Info($"4.0. Don hang chieu RA: order = null");
                return false;
            }

            foreach (var order in orders) { 
                _logger.Info($"4.0. Kiem tra don hang chieu RA: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");
            }

            var isValid = orders.Any(x => (x.Step == (int)OrderStep.DA_CAN_RA
                                       && (x.DriverUserName ?? "") != "")
                                    );

            return isValid;
        }

        public static string CheckValidOrdersExitGateway(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                _logger.Info($"4.0. Don hang chieu RA: order = null");

                return CheckValidRfidResultCode.CHUA_CO_DON;
            }

            foreach (var order in orders)
            {
                _logger.Info($"4.0. Kiem tra don hang chieu RA: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");
            }

            var isValid = orders.Any(x => (x.Step == (int)OrderStep.DA_CAN_RA
                                       && (x.DriverUserName ?? "") != "")
                                    );

            if (isValid)
            {
                return CheckValidRfidResultCode.HOP_LE;
            }
            else
            {
                return CheckValidRfidResultCode.CHUA_CAN_RA;
            }
        }

        public static List<tblStoreOrderOperating> ValidOrdersExitGateway(List<tblStoreOrderOperating> orders)
        {
            if (orders == null)
            {
                return null;
            }

            var validOrders = orders.Where(x => (x.Step == (int)OrderStep.DA_CAN_RA
                                       && (x.DriverUserName ?? "") != "")
                                    )
                                    .ToList();

            return validOrders;
        }

        public static bool IsValidOrderScaleStation(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang tai can: order = null");
                return false;
            }

            _logger.Info($"4.0. Kiem tra don hang tai can: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}, WeightIn = {order.WeightIn}, SumNumber = {order.SumNumber}");

            if (order.CatId == OrderCatIdCode.XI_MANG_XA)
            {
                if (!string.IsNullOrEmpty(order.DriverUserName)
                    && 
                    (
                        order.Step == (int)OrderStep.DA_XAC_THUC
                        ||
                        order.Step == (int)OrderStep.CHO_GOI_XE
                        ||
                        order.Step == (int)OrderStep.DANG_GOI_XE
                        ||
                        order.Step == (int)OrderStep.DA_VAO_CONG
                    )
                )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(order.DriverUserName)
                    &&
                    (
                        order.Step == (int)OrderStep.DA_XAC_THUC
                        ||
                        order.Step == (int)OrderStep.CHO_GOI_XE
                        ||
                        order.Step == (int)OrderStep.DANG_GOI_XE
                        ||
                        order.Step == (int)OrderStep.DA_VAO_CONG
                        ||
                        order.Step == (int)OrderStep.DA_CAN_VAO
                        ||
                        order.Step == (int)OrderStep.DANG_LAY_HANG
                        ||
                        order.Step == (int)OrderStep.DA_LAY_HANG
                    )
                )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static string CheckValidOrderScaleStation(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang tai can: order = null");

                return CheckValidRfidResultCode.CHUA_CO_DON;
            }

            _logger.Info($"4.0. Kiem tra don hang tai can: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}, WeightIn = {order.WeightIn}, SumNumber = {order.SumNumber}");

            var isValid = false;
            var isValidConfirm = false;

            isValid = order.Step == (int)OrderStep.DA_NHAN_DON && (order.DriverUserName ?? "") != "";

            isValidConfirm = order.Step == (int)OrderStep.DA_XAC_THUC && (order.DriverUserName ?? "") != "";

            if (isValid)
            {
                return CheckValidRfidResultCode.HOP_LE;
            }
            else if (isValidConfirm)
            {
                return CheckValidRfidResultCode.DA_XAC_THUC;
            }
            else
            {
                return CheckValidRfidResultCode.CHUA_NHAN_DON;
            }
        }

        public static bool IsValidOrderConfirmationPoint(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang: order = null");
                return false;
            }

            _logger.Info($"4.0. Kiem tra don hang: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            if (
                order.Step == (int)OrderStep.DA_NHAN_DON
                && (order.DriverUserName ?? "") != ""
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string CheckValidOrderConfirmationPoint(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang: order = null");

                return CheckValidRfidResultCode.CHUA_CO_DON;
            }

            _logger.Info($"4.0. Kiem tra don hang: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            var isValid = order.Step == (int)OrderStep.DA_NHAN_DON && (order.DriverUserName ?? "") != "";

            var isValidConfirm = order.Step == (int)OrderStep.DA_XAC_THUC && (order.DriverUserName ?? "") != "";

            if (isValid)
            {
                return CheckValidRfidResultCode.HOP_LE;
            }
            else if (isValidConfirm)
            {
                return CheckValidRfidResultCode.DA_XAC_THUC;
            }
            else
            {
                return CheckValidRfidResultCode.CHUA_NHAN_DON;
            }
        }
    }
}
