﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class XHTD_Entities : DbContext
    {
        public XHTD_Entities()
            : base("name=XHTD_Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<tblAccount> tblAccounts { get; set; }
        public virtual DbSet<tblAccountGroup> tblAccountGroups { get; set; }
        public virtual DbSet<tblCategory> tblCategories { get; set; }
        public virtual DbSet<tblCategoriesDevice> tblCategoriesDevices { get; set; }
        public virtual DbSet<tblConfigApp> tblConfigApps { get; set; }
        public virtual DbSet<tblDevice> tblDevices { get; set; }
        public virtual DbSet<tblDeviceGroup> tblDeviceGroups { get; set; }
        public virtual DbSet<tblDeviceOperating> tblDeviceOperatings { get; set; }
        public virtual DbSet<tblDriver> tblDrivers { get; set; }
        public virtual DbSet<tblDriverVehicle> tblDriverVehicles { get; set; }
        public virtual DbSet<tblNotification> tblNotifications { get; set; }
        public virtual DbSet<tblRfid> tblRfids { get; set; }
        public virtual DbSet<tblRfidSign> tblRfidSigns { get; set; }
        public virtual DbSet<tblStoreOrderOperating> tblStoreOrderOperatings { get; set; }
        public virtual DbSet<tblStoreOrderOperatingPriority> tblStoreOrderOperatingPriorities { get; set; }
        public virtual DbSet<tblTrough> tblTroughs { get; set; }
        public virtual DbSet<tblTroughTypeProduct> tblTroughTypeProducts { get; set; }
        public virtual DbSet<tblVehicle> tblVehicles { get; set; }
        public virtual DbSet<tblCompany> tblCompanies { get; set; }
        public virtual DbSet<tblStoreOrderOperatingVoice> tblStoreOrderOperatingVoices { get; set; }
    }
}
