using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES.Data.Repositories
{
    public class VehicleRepository : BaseRepository <tblVehicle>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public VehicleRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public bool CheckExist(string vehicleCode)
        {
            var vehicleExist = _appDbContext.tblVehicles.FirstOrDefault(x => x.Vehicle == vehicleCode);
            if (vehicleExist != null)
            {
                return true;
            }
            return false;
        }

        public async Task CreateAsync(string vehicleCode)
        {
            try
            {
                if (!CheckExist(vehicleCode))
                {
                    var newItem = new tblVehicle
                    {
                        Vehicle = vehicleCode,
                    };

                    _appDbContext.tblVehicles.Add(newItem);
                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine("Them moi phuong tien: " + vehicleCode);
                }
                else
                {
                    Console.WriteLine("Da ton tai phuong tien: " + vehicleCode);
                }
            }
            catch (Exception ex)
            {
                log.Error("CreateAsync vehicle log Error: " + ex.Message); ;
                Console.WriteLine("CreateAsync vehicle log Error: " + ex.Message);
            }
        }

        public async Task UpdateUnladenWeight(string rfid, int weight)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var rfidRecord = dbContext.tblRfids.FirstOrDefault(x => x.Code == rfid);
                if (rfidRecord == null)
                {
                    return;
                }

                var vehicle = rfidRecord.Vehicle;

                var vehicleRecord = dbContext.tblVehicles.FirstOrDefault(x => x.Vehicle == vehicle);
                if(vehicleRecord == null)
                {
                    return;
                }

                var number = 1;

                var lastUpdate1 = vehicleRecord.UnladenWeight1LastUpdate;
                var lastUpdate2 = vehicleRecord.UnladenWeight2LastUpdate;
                var lastUpdate3 = vehicleRecord.UnladenWeight3LastUpdate;

                if (lastUpdate1 == null)
                {
                    number = 1;
                } 
                else if (lastUpdate2 == null)
                {
                    number = 2;
                }
                else if (lastUpdate3 == null)
                {
                    number = 3;
                }
                else
                {
                    var min = lastUpdate1;
                    if (min > vehicleRecord.UnladenWeight2LastUpdate)
                    {
                        min = vehicleRecord.UnladenWeight2LastUpdate;
                        number = 2;
                    }

                    if (min > vehicleRecord.UnladenWeight3LastUpdate)
                    {
                        number = 3;
                    }
                }
                
                if(number == 1)
                {
                    vehicleRecord.UnladenWeight1 = weight;
                    vehicleRecord.UnladenWeight1LastUpdate = DateTime.Now;
                    
                }
                else if (number == 2)
                {
                    vehicleRecord.UnladenWeight2 = weight;
                    vehicleRecord.UnladenWeight2LastUpdate = DateTime.Now;
                }
                else if (number == 3)
                {
                    vehicleRecord.UnladenWeight3 = weight;
                    vehicleRecord.UnladenWeight3LastUpdate = DateTime.Now;
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
