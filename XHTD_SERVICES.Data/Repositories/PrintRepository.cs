using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES.Data.Repositories
{
    public class PrintRepository : BaseRepository<TblPrint>
    {
        public PrintRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<List<TblPrint>> GetByStatus(string status)
        {
            using (var dbContext = new XHTD_Entities())
            {
                return await dbContext.TblPrints.Where(x => x.Status == status).ToListAsync();
            }
        }

        public async Task UpdateRange(List<TblPrint> prints)
        {
            using (var dbContext = new XHTD_Entities())
            {
                foreach (var print in prints)
                {
                      dbContext.TblPrints.AddOrUpdate(print);
                }

                await dbContext.SaveChangesAsync();   
            }
        }
    }
}
