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
    
    public partial class tblCheckInOut
    {
        public int Id { get; set; }
        public Nullable<System.DateTime> CheckInTime { get; set; }
        public Nullable<System.DateTime> CheckOutTime { get; set; }
        public Nullable<int> AttactmentId { get; set; }
        public string LogProcess { get; set; }
        public string Vehicle { get; set; }
    
        public virtual tblAttachment tblAttachment { get; set; }
    }
}
