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
    
    public partial class TblQualityCertificate
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TblQualityCertificate()
        {
            this.TblQualityCertificatePartners = new HashSet<TblQualityCertificatePartner>();
        }
    
        public int Id { get; set; }
        public string Code { get; set; }
        public System.DateTime FromDate { get; set; }
        public System.DateTime ToDate { get; set; }
        public double ExpectNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string State { get; set; }
        public string Note { get; set; }
        public Nullable<System.Guid> TempReferenceId { get; set; }
        public Nullable<System.Guid> ReferenceId { get; set; }
        public Nullable<System.DateTime> CreateDay { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> UpdateDay { get; set; }
        public string UpdateBy { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TblQualityCertificatePartner> TblQualityCertificatePartners { get; set; }
    }
}
