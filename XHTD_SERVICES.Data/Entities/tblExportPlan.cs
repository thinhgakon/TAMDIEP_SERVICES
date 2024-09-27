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
    
    public partial class tblExportPlan
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tblExportPlan()
        {
            this.tblExportPlanDetails = new HashSet<tblExportPlanDetail>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShipName { get; set; }
        public Nullable<System.DateTime> StartDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
        public int Status { get; set; }
        public int SourceDocumentId { get; set; }
        public string OrderType { get; set; }
        public string SourceDocumentName { get; set; }
        public string PlanType { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tblExportPlanDetail> tblExportPlanDetails { get; set; }
    }
}
