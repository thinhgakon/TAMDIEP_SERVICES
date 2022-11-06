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
    public class CategoriesDevicesLogRepository : BaseRepository <tblCategoriesDevicesLog>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CategoriesDevicesLogRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task CreateAsync(CategoriesDevicesLogItemResponse item)
        {
            try {
                var newLog = new tblCategoriesDevicesLog
                {
                    Code = item.Code,
                    ActionType = item.ActionType,
                    ActionInfo = item.ActionInfo,
                    ActionDate = item.ActionDate,
                };

                _appDbContext.tblCategoriesDevicesLogs.Add(newLog);
                await _appDbContext.SaveChangesAsync();

                Console.WriteLine($@"Inserted device log");
                log.Info($@"Inserted device log");
            }
            catch(Exception ex)
            {
                log.Error("CreateAsync device log Error: " + ex.Message); ;
                Console.WriteLine("CreateAsync device log Error: " + ex.Message);
            }
        }
    }
}
