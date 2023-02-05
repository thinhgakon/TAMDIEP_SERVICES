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

        public async Task<List<string>> GetAllMachineCodes()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var machines = await dbContext.tblMachines
                                    .Where(x => x.State == true)
                                    .OrderBy(x => x.Id)
                                    .Select(x => x.Code)
                                    .ToListAsync();

                return machines;
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
