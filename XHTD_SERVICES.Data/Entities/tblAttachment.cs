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
    
    public partial class tblAttachment
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tblAttachment()
        {
            this.tblCheckInOuts = new HashSet<tblCheckInOut>();
        }
    
        public int Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Thumbnail { get; set; }
        public string Extension { get; set; }
        public Nullable<double> Size { get; set; }
        public string Type { get; set; }
        public string CreateBy { get; set; }
        public string UpdateBy { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> UpdateDate { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tblCheckInOut> tblCheckInOuts { get; set; }
    }
}
