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
    public class MachineRepository : BaseRepository <tblMachine>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MachineRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<List<string>> GetActiveXiBaoMachines()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var query = from v in dbContext.tblMachines
                            join r in dbContext.tblMachineTypeProducts
                            on v.Code equals r.MachineCode
                            where
                                v.State == true
                                && (r.TypeProduct == "PCB30" || r.TypeProduct == "PCB40")
                            orderby v.Id ascending
                            select v.Code;

                var troughts = await query.Distinct().ToListAsync();

                return troughts;
            }
        }

        public async Task<bool> IsWorkingMachine(string machineCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Machine == machineCode && x.Working == true);
                if(trough != null)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
