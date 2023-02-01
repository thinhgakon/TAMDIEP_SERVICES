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

        public async Task UpdateMachine(string troughCode, string deliveryCode, double countQuantity, double planQuantity)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == troughCode);

                    if (trough != null)
                    {
                        var machine = await dbContext.tblMachines.FirstOrDefaultAsync(x => x.Code == trough.Machine);
                        if (machine != null)
                        {
                            machine.Working = true;
                            machine.DeliveryCodeCurrent = deliveryCode;
                            machine.CountQuantityCurrent = countQuantity;
                            machine.PlanQuantityCurrent = planQuantity;

                            await dbContext.SaveChangesAsync();

                            log.Info($@"UpdateMachine Success");
                            Console.WriteLine($@"UpdateMachine Success");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateMachine Error: " + ex.Message);
                    Console.WriteLine($@"UpdateMachine Error: " + ex.Message);
                }
            }
        }
    }
}
