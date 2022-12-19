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
    public class TroughRepository : BaseRepository <tblTrough>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TroughRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<List<string>> GetActiveXiBaoTroughs()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var query = from v in dbContext.tblTroughs
                            join r in dbContext.tblTroughTypeProducts
                            on v.Code equals r.TroughCode
                            where
                                v.State == true 
                                &&  (r.TypeProduct == "PCB30" || r.TypeProduct == "PCB40")
                            select v.Code;

                var troughts = await query.Distinct().ToListAsync();

                return troughts;
            }
        }

        public tblTrough GetDetail(string code)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = dbContext.tblTroughs.FirstOrDefault(x => x.Code == code && x.State == true);

                return trough;
            }
        }
        public async Task<List<string>> GetAllTroughCode()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.Select(x=>x.Code).ToListAsync();

                return trough;
            }
        }
        public async Task<bool> IsDelivering(string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x=>x.Code == troughCode);
                return trough?.DeliveryCodeCurrent == null ? false : true;
            }

        }

    }
}
