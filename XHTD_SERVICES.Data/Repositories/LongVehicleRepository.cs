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
    public class LongVehicleRepository : BaseRepository <tblLongVehicle>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LongVehicleRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<bool> CheckExist(string vehicleCode)
        {
            var vehicleExist = await _appDbContext.tblLongVehicles.FirstOrDefaultAsync(x => x.Vehicle == vehicleCode);
            if (vehicleExist != null)
            {
                return true;
            }
            return false;
        }
    }
}
