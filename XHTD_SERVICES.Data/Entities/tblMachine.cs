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
    
    public partial class tblMachine
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tblMachine()
        {
            this.TblMachineTroughs = new HashSet<TblMachineTrough>();
            this.tblMachineTypeProducts = new HashSet<tblMachineTypeProduct>();
        }
    
        public string Code { get; set; }
        public string Name { get; set; }
        public Nullable<bool> State { get; set; }
        public Nullable<System.DateTime> CreateDay { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> UpdateDay { get; set; }
        public string UpdateBy { get; set; }
        public string ProductCategory { get; set; }
        public string CurrentDeliveryCode { get; set; }
        public string StartStatus { get; set; }
        public string StopStatus { get; set; }
        public Nullable<double> StartCountingFrom { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TblMachineTrough> TblMachineTroughs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tblMachineTypeProduct> tblMachineTypeProducts { get; set; }
    }
}
