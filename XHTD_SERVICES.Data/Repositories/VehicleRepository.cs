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
    }
}
