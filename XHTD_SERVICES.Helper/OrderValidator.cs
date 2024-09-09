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

            if (order.CatId == OrderCatIdCode.CLINKER)
            {
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
            else if (order.TypeXK == OrderTypeXKCode.JUMBO || order.TypeXK == OrderTypeXKCode.SLING)
            {
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
            else
            {
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
        }

        public static bool IsValidOrderExitGateway(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang chieu RA: order = null");
                return false;
            }

            _logger.Info($"4.0. Kiem tra don hang chieu RA: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            if (order.CatId == OrderCatIdCode.CLINKER)
            {
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
            else if (order.TypeXK == OrderTypeXKCode.JUMBO || order.TypeXK == OrderTypeXKCode.SLING)
            {
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
            else
            {
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
                    && order.Step != (int)OrderStep.CHUA_NHAN_DON
                    && order.Step != (int)OrderStep.DA_NHAN_DON
                    && order.Step != (int)OrderStep.DA_CAN_VAO
                    && order.Step != (int)OrderStep.DANG_LAY_HANG
                    && order.Step != (int)OrderStep.DA_LAY_HANG
                    && order.Step != (int)OrderStep.DA_CAN_RA
                    && order.Step != (int)OrderStep.DA_HOAN_THANH
                    && order.Step != (int)OrderStep.DA_GIAO_HANG
                    )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (order.CatId == OrderCatIdCode.CLINKER)
            {
                if (!string.IsNullOrEmpty(order.DriverUserName)
                    && order.Step != (int)OrderStep.CHUA_NHAN_DON
                    && order.Step != (int)OrderStep.DA_NHAN_DON
                    && order.Step != (int)OrderStep.DA_CAN_RA
                    && order.Step != (int)OrderStep.DA_HOAN_THANH
                    && order.Step != (int)OrderStep.DA_GIAO_HANG
                    )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (order.TypeXK == OrderTypeXKCode.JUMBO || order.TypeXK == OrderTypeXKCode.SLING)
            {
                if (!string.IsNullOrEmpty(order.DriverUserName)
                    && order.Step != (int)OrderStep.CHUA_NHAN_DON
                    && order.Step != (int)OrderStep.DA_NHAN_DON
                    && order.Step != (int)OrderStep.DA_CAN_RA
                    && order.Step != (int)OrderStep.DA_HOAN_THANH
                    && order.Step != (int)OrderStep.DA_GIAO_HANG
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
                    && order.Step != (int)OrderStep.CHUA_NHAN_DON 
                    && order.Step != (int)OrderStep.DA_NHAN_DON
                    && order.Step != (int)OrderStep.DA_CAN_RA
                    && order.Step != (int)OrderStep.DA_HOAN_THANH
                    && order.Step != (int)OrderStep.DA_GIAO_HANG
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

        public static bool IsValidOrderConfirmationPoint(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.Info($"4.0. Don hang: order = null");
                return false;
            }

            _logger.Info($"4.0. Kiem tra don hang: DeliveryCode = {order.DeliveryCode}, CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            if (order.CatId == OrderCatIdCode.CLINKER)
            {
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
            else if (order.TypeXK == OrderTypeXKCode.JUMBO || order.TypeXK == OrderTypeXKCode.SLING)
            {
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
            else
            {
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
        }
    }
}
