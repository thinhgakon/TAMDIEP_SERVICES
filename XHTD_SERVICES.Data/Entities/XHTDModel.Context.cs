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
        public virtual DbSet<tblAccountGroupFunction> tblAccountGroupFunctions { get; set; }
        public virtual DbSet<tblCallToTrough> tblCallToTroughs { get; set; }
        public virtual DbSet<tblCallVehicleStatu> tblCallVehicleStatus { get; set; }
        public virtual DbSet<tblCategory> tblCategories { get; set; }
        public virtual DbSet<tblCategoriesDevice> tblCategoriesDevices { get; set; }
        public virtual DbSet<tblCategoriesDevicesLog> tblCategoriesDevicesLogs { get; set; }
        public virtual DbSet<tblConfigApp> tblConfigApps { get; set; }
        public virtual DbSet<tblDevice> tblDevices { get; set; }
        public virtual DbSet<tblDeviceGroup> tblDeviceGroups { get; set; }
        public virtual DbSet<tblDeviceOperating> tblDeviceOperatings { get; set; }
        public virtual DbSet<tblDriver> tblDrivers { get; set; }
        public virtual DbSet<tblDriverVehicle> tblDriverVehicles { get; set; }
        public virtual DbSet<tblFunction> tblFunctions { get; set; }
        public virtual DbSet<tblLongVehicle> tblLongVehicles { get; set; }
        public virtual DbSet<tblMachine> tblMachines { get; set; }
        public virtual DbSet<tblMachineTypeProduct> tblMachineTypeProducts { get; set; }
        public virtual DbSet<tblNotification> tblNotifications { get; set; }
        public virtual DbSet<tblPrintConfig> tblPrintConfigs { get; set; }
        public virtual DbSet<tblRfid> tblRfids { get; set; }
        public virtual DbSet<tblRfidSign> tblRfidSigns { get; set; }
        public virtual DbSet<tblScaleOperating> tblScaleOperatings { get; set; }
        public virtual DbSet<tblStoreOrderLocation> tblStoreOrderLocations { get; set; }
        public virtual DbSet<tblStoreOrderOperating> tblStoreOrderOperatings { get; set; }
        public virtual DbSet<tblStoreOrderOperatingPriority> tblStoreOrderOperatingPriorities { get; set; }
        public virtual DbSet<tblSystemParameter> tblSystemParameters { get; set; }
        public virtual DbSet<tblTrough> tblTroughs { get; set; }
        public virtual DbSet<tblTroughTypeProduct> tblTroughTypeProducts { get; set; }
        public virtual DbSet<tblTypeProduct> tblTypeProducts { get; set; }
        public virtual DbSet<tblVehicle> tblVehicles { get; set; }
        public virtual DbSet<tblCompany> tblCompanies { get; set; }
        public virtual DbSet<tblStoreOrderOperatingVoice> tblStoreOrderOperatingVoices { get; set; }
        public virtual DbSet<tblCheckInOut> tblCheckInOuts { get; set; }
        public virtual DbSet<tblAttachment> tblAttachments { get; set; }
        public virtual DbSet<TblMachineTrough> TblMachineTroughs { get; set; }
        public virtual DbSet<TblPrint> TblPrints { get; set; }
        public virtual DbSet<tblExportHistory> tblExportHistories { get; set; }
        public virtual DbSet<tblCallToGatewayConfig> tblCallToGatewayConfigs { get; set; }
        public virtual DbSet<tblExportPlan> tblExportPlans { get; set; }
        public virtual DbSet<tblExportPlanDetail> tblExportPlanDetails { get; set; }
        public virtual DbSet<ItemFormula> ItemFormulas { get; set; }
        public virtual DbSet<Item> Items { get; set; }
        public virtual DbSet<tblConfigOperating> tblConfigOperatings { get; set; }
        public virtual DbSet<tblItemConfig> tblItemConfigs { get; set; }
        public virtual DbSet<TblNotificationGroup> TblNotificationGroups { get; set; }
        public virtual DbSet<TblSendTroughStatu> TblSendTroughStatus { get; set; }
        public virtual DbSet<tblStoreOrderOperatingHistory> tblStoreOrderOperatingHistories { get; set; }
        public virtual DbSet<TblQualityCertificate> TblQualityCertificates { get; set; }
        public virtual DbSet<TblQualityCertificateCCCL> TblQualityCertificateCCCLs { get; set; }
        public virtual DbSet<TblQualityCertificateCCCLProcess> TblQualityCertificateCCCLProcesses { get; set; }
        public virtual DbSet<tblTypeProductCallToGatewayConfig> tblTypeProductCallToGatewayConfigs { get; set; }
    }
}
