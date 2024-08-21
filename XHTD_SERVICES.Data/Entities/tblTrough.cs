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
    
    public partial class tblTrough
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tblTrough()
        {
            this.tblTroughTypeProducts = new HashSet<tblTroughTypeProduct>();
            this.TblMachineTroughs = new HashSet<TblMachineTrough>();
        }
    
        public Nullable<int> Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Machine { get; set; }
        public Nullable<double> Height { get; set; }
        public Nullable<double> Width { get; set; }
        public Nullable<double> Long { get; set; }
        public Nullable<bool> Working { get; set; }
        public Nullable<bool> Problem { get; set; }
        public Nullable<bool> State { get; set; }
        public string DeliveryCodeCurrent { get; set; }
        public Nullable<double> PlanQuantityCurrent { get; set; }
        public Nullable<double> CountQuantityCurrent { get; set; }
        public Nullable<bool> IsPicking { get; set; }
        public string TransportNameCurrent { get; set; }
        public Nullable<System.DateTime> CheckInTimeCurrent { get; set; }
        public Nullable<bool> IsInviting { get; set; }
        public string LineCode { get; set; }
        public Nullable<System.DateTime> CreateDay { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> UpdateDay { get; set; }
        public string UpdateBy { get; set; }
        public string ProductCategory { get; set; }
        public Nullable<double> FirstSensorQuantityCurrent { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tblTroughTypeProduct> tblTroughTypeProducts { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TblMachineTrough> TblMachineTroughs { get; set; }
    }
}
