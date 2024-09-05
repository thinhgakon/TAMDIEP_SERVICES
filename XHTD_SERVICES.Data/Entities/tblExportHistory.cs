//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace XHTD_SERVICES.Data.Entities
{
    using System;
    using System.Collections.Generic;
    
    public partial class tblExportHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string DeliveryCode { get; set; }
        public string TroughCode { get; set; }
        public string MachineCode { get; set; }
        public Nullable<double> CountQuantityStart { get; set; }
        public Nullable<double> CountQuantityEnd { get; set; }
        public Nullable<System.DateTime> TimeStart { get; set; }
        public Nullable<System.DateTime> TimeEnd { get; set; }
        public Nullable<decimal> MachineExportedNumber { get; set; }
        public Nullable<int> RemainingCountQuantity { get; set; }
        public Nullable<double> FirstSensorCountQuantityEnd { get; set; }
        public Nullable<double> FirstSensorCountQuantityStart { get; set; }
    
        public virtual tblStoreOrderOperating tblStoreOrderOperating { get; set; }
    }
}
