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
    
    public partial class tblRfid
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Vehicle { get; set; }
        public System.DateTime DayReleased { get; set; }
        public System.DateTime DayExpired { get; set; }
        public string Note { get; set; }
        public Nullable<bool> State { get; set; }
        public Nullable<System.DateTime> LastEnter { get; set; }
        public Nullable<System.DateTime> CreateDay { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> UpdateDay { get; set; }
        public string UpdateBy { get; set; }
    }
}
