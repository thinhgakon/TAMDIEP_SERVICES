using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;

namespace XHTD_SERVICES.Data.Repositories
{
    public class TroughRepository : BaseRepository <tblTrough>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TroughRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public tblTrough GetDetail(string code)
        {
            var trough = _appDbContext.tblTroughs.FirstOrDefault(x => x.Code == code);

            return trough;
        }
    }
}
