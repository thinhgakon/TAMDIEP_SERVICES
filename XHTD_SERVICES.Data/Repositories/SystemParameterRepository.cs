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
    public class SystemParameterRepository : BaseRepository <tblSystemParameter>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SystemParameterRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<List<tblSystemParameter>> GetSystemParameters()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var parameters = await dbContext.tblSystemParameters.ToListAsync();
                return parameters;
            }
        }
    }
}
