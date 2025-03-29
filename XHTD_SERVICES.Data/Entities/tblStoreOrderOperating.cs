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
    
    public partial class tblStoreOrderOperating
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tblStoreOrderOperating()
        {
            this.tblExportHistories = new HashSet<tblExportHistory>();
        }
    
        public int Id { get; set; }
        public string Vehicle { get; set; }
        public string DriverName { get; set; }
        public string NameDistributor { get; set; }
        public Nullable<double> ItemId { get; set; }
        public string NameProduct { get; set; }
        public string CatId { get; set; }
        public Nullable<decimal> SumNumber { get; set; }
        public Nullable<System.DateTime> TimeIn33 { get; set; }
        public string CardNo { get; set; }
        public Nullable<int> OrderId { get; set; }
        public string DeliveryCode { get; set; }
        public string DeliveryCodeParent { get; set; }
        public Nullable<System.DateTime> OrderDate { get; set; }
        public string TypeProduct { get; set; }
        public string TypeXK { get; set; }
        public Nullable<System.DateTime> TimeIn21 { get; set; }
        public Nullable<System.DateTime> TimeIn22 { get; set; }
        public Nullable<int> Confirm1 { get; set; }
        public Nullable<System.DateTime> TimeConfirm1 { get; set; }
        public Nullable<int> Confirm2 { get; set; }
        public Nullable<System.DateTime> TimeConfirm2 { get; set; }
        public Nullable<int> Confirm3 { get; set; }
        public Nullable<System.DateTime> TimeConfirm3 { get; set; }
        public Nullable<int> Confirm4 { get; set; }
        public Nullable<System.DateTime> TimeConfirm4 { get; set; }
        public Nullable<int> Confirm5 { get; set; }
        public Nullable<System.DateTime> TimeConfirm5 { get; set; }
        public Nullable<int> Confirm6 { get; set; }
        public Nullable<System.DateTime> TimeConfirm6 { get; set; }
        public Nullable<int> Confirm7 { get; set; }
        public Nullable<System.DateTime> TimeConfirm7 { get; set; }
        public Nullable<int> Confirm8 { get; set; }
        public Nullable<System.DateTime> TimeConfirm8 { get; set; }
        public Nullable<int> Confirm9 { get; set; }
        public Nullable<System.DateTime> TimeConfirm9 { get; set; }
        public string Confirm9Note { get; set; }
        public string Confirm9Image { get; set; }
        public Nullable<int> Step { get; set; }
        public Nullable<int> IndexOrder { get; set; }
        public Nullable<int> IndexOrder1 { get; set; }
        public Nullable<int> IndexOrder2 { get; set; }
        public Nullable<int> Trough { get; set; }
        public Nullable<int> Trough1 { get; set; }
        public Nullable<int> NumberVoice { get; set; }
        public string State { get; set; }
        public Nullable<int> Prioritize { get; set; }
        public Nullable<System.DateTime> DayCreate { get; set; }
        public Nullable<int> IDDistributorSyn { get; set; }
        public Nullable<int> AreaId { get; set; }
        public string AreaName { get; set; }
        public string CodeStore { get; set; }
        public string NameStore { get; set; }
        public string DriverUserName { get; set; }
        public Nullable<System.DateTime> DriverAccept { get; set; }
        public Nullable<int> IndexOrderTemp { get; set; }
        public Nullable<int> WeightIn { get; set; }
        public Nullable<System.DateTime> WeightInTime { get; set; }
        public Nullable<int> WeightOut { get; set; }
        public Nullable<System.DateTime> WeightOutTime { get; set; }
        public Nullable<int> WeightInAuto { get; set; }
        public Nullable<System.DateTime> WeightInTimeAuto { get; set; }
        public Nullable<int> WeightOutAuto { get; set; }
        public Nullable<System.DateTime> WeightOutTimeAuto { get; set; }
        public string NoteFinish { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public Nullable<int> CountReindex { get; set; }
        public Nullable<bool> IsVoiced { get; set; }
        public string LocationCode { get; set; }
        public Nullable<int> TransportMethodId { get; set; }
        public string TransportMethodName { get; set; }
        public Nullable<bool> LockInDbet { get; set; }
        public string LogJobAttach { get; set; }
        public Nullable<bool> IsSyncedByNewWS { get; set; }
        public string TroughLineCode { get; set; }
        public Nullable<bool> IsScaleAuto { get; set; }
        public Nullable<System.DateTime> TimeConfirmHistory { get; set; }
        public string LogHistory { get; set; }
        public string MoocCode { get; set; }
        public string LogProcessOrder { get; set; }
        public Nullable<int> DriverPrintNumber { get; set; }
        public Nullable<System.DateTime> DriverPrintTime { get; set; }
        public Nullable<bool> WarningNotCall { get; set; }
        public string XiRoiAttatchmentFile { get; set; }
        public string PackageNumber { get; set; }
        public Nullable<int> Shifts { get; set; }
        public Nullable<bool> AutoScaleOut { get; set; }
        public Nullable<System.DateTime> CreateDay { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> UpdateDay { get; set; }
        public string UpdateBy { get; set; }
        public Nullable<int> Confirm10 { get; set; }
        public Nullable<System.DateTime> TimeConfirm10 { get; set; }
        public Nullable<System.Guid> ReferenceId { get; set; }
        public string ImgConfirm10 { get; set; }
        public string DocNum { get; set; }
        public string ErpOrderId { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceStatus { get; set; }
        public Nullable<decimal> ExportedNumber { get; set; }
        public Nullable<decimal> ExtraNumber { get; set; }
        public string ExtraReason { get; set; }
        public Nullable<decimal> MachineExportedNumber { get; set; }
        public Nullable<int> Confirm11 { get; set; }
        public Nullable<System.DateTime> TimeConfirm11 { get; set; }
        public string LotNumber { get; set; }
        public string LocationCodeTgc { get; set; }
        public string SealNumber { get; set; }
        public Nullable<bool> IsScaleInAuto { get; set; }
        public Nullable<int> SourceDocumentId { get; set; }
        public string ItemAlias { get; set; }
        public Nullable<double> NetWeight { get; set; }
        public Nullable<bool> IsScaleInVirtual { get; set; }
        public Nullable<bool> IsScaleOutVirtual { get; set; }
        public Nullable<System.DateTime> WeightInTimeVirtual { get; set; }
        public Nullable<int> WeightInVirtual { get; set; }
        public Nullable<System.DateTime> WeightOutTimeVirtual { get; set; }
        public Nullable<int> WeightOutVirtual { get; set; }
        public Nullable<bool> IsSyncedOutSource1 { get; set; }
        public Nullable<bool> IsSyncedOutSource2 { get; set; }
        public Nullable<int> SealCount { get; set; }
        public string SealDes { get; set; }
        public string DeliveryCodeTgc { get; set; }
        public Nullable<bool> IsFromWeightOut { get; set; }
        public string PrintMachineCode { get; set; }
        public Nullable<System.DateTime> StartPrintData { get; set; }
        public Nullable<System.DateTime> StopPrintData { get; set; }
        public string PrintTroughCode { get; set; }
        public Nullable<decimal> RealNumber { get; set; }
        public string ErrorLog { get; set; }
        public string AreaCode { get; set; }
        public string SourceDocumentName { get; set; }
        public Nullable<int> Type { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tblExportHistory> tblExportHistories { get; set; }
    }
}
