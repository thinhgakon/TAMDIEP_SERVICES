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
    public class CategoriesDevicesRepository : BaseRepository <tblCategoriesDevice>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CategoriesDevicesRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<List<tblCategoriesDevice>> GetDevices(string catCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var devices = await dbContext.tblCategoriesDevices.Where(x => x.CatCode == catCode).ToListAsync();
                return devices;
            }
        }
    }
}
