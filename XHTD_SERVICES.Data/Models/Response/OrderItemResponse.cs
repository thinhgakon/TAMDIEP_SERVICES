using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Data.Models.Response
{
    public class OrderItemResponse
    {
        public double? ouId { get; set; }
        public int? id { get; set; }
        public string deliveryCode { get; set; }
        public string deliveryCodeTgc { get; set; }
        public string orderDate { get; set; }
        public string deliveryDate { get; set; }
        public string customerId { get; set; }
        public string branchId { get; set; }
        public string branchName { get; set; }
        public string customerNumber { get; set; }
        public string customerName { get; set; }
        public string customerFullName { get; set; }
        public string customerAddress { get; set; }
        public string taxReference { get; set; }
        public string contractId { get; set; }
        public string contractNumber { get; set; }
        public string contractDate { get; set; }
        public string productId { get; set; }
        public string productCode { get; set; }
        public string productName { get; set; }
        public string itemCategory { get; set; }
        public string shippointId { get; set; }
        public string shippointName { get; set; }
        public string checkpointId { get; set; }
        public string checkpointName { get; set; }
        public string areaId { get; set; }
        public string areaName { get; set; }
        public string areaCode { get; set; }
        public string rootAreaCode { get; set; }
        public decimal? bookQuantity { get; set; }
        public decimal? orderQuantity { get; set; }
        public string realQuantity { get; set; }
        public string orderAmount { get; set; }
        public string priceId { get; set; }
        public string unitPrice { get; set; }
        public string currency { get; set; }
        public string uomCode { get; set; }
        public string locationCode { get; set; }
        public int? transportMethodId { get; set; }
        public string transportMethodName { get; set; }
        public string transportMethodTypeId { get; set; }
        public string transportMethodTypeName { get; set; }
        public string orderType { get; set; }
        public object orderTypeName { get; set; }
        public string timeIn { get; set; }
        public string timeOut { get; set; }
        public string vehicleCode { get; set; }
        public string vehicleLimit { get; set; }
        public string moocLimit { get; set; }
        public string moocCode { get; set; }
        public string driverName { get; set; }
        public string status { get; set; }
        public string orderPrintStatus { get; set; }
        public string productionLine { get; set; }
        public string shipFromWareHouse { get; set; }
        public string documentNumber { get; set; }
        public string documentSeries { get; set; }
        public string lotNumber { get; set; }
        public string docTemplate { get; set; }
        public string description { get; set; }
        public string sourceDocumentId { get; set; }
        public string sourceDocumentType { get; set; }
        public string driverIndex { get; set; }
        public string packType { get; set; }
        public string bagType { get; set; }
        public string bagNum { get; set; }
        public string orderLog { get; set; }
        public string driverLicense { get; set; }
        public string freightName { get; set; }
        public string customerWarehouseId { get; set; }
        public string customerWarehouseName { get; set; }
        public string discount { get; set; }
        public string userCreate { get; set; }
        public string note { get; set; }
        public string totalAmount { get; set; }
        public string vatAmount { get; set; }
        public string loadweightnull { get; set; }
        public string loadweightfull { get; set; }
        public string topSealCount { get; set; }
        public string topSealDes { get; set; }
        public string createDate { get; set; }
        public string lastUpdatedDate { get; set; }
        public string dmsId { get; set; }
        public string docnum { get; set; }
        public string locationCodeTgc { get; set; }
        public string itemalias { get; set; }
        public string netweight { get; set; }
        public int? sideSealCount { get; set; }
        public string sideSealDes { get; set; }
    }
}
