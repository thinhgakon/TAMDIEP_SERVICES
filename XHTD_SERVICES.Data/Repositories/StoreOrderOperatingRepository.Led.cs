using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository
    {
        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangRoiIndexed()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == OrderCatIdCode.XI_MANG_XA
                                                && x.IsVoiced == false
                                                && x.IndexOrder > 0
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangRoiNoIndex()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == OrderCatIdCode.XI_MANG_XA
                                                && x.IsVoiced == false
                                                //&& x.TimeConfirm3 < DateTime.Now.AddMinutes(-2)
                                                && (x.IndexOrder == null || x.IndexOrder == 0)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangBaoIndexed()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == OrderCatIdCode.XI_MANG_BAO
                                                && x.IsVoiced == false
                                                && x.IndexOrder > 0
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangBaoNoIndex()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == OrderCatIdCode.XI_MANG_BAO
                                                && x.IsVoiced == false
                                                //&& x.TimeConfirm3 < DateTime.Now.AddMinutes(-2)
                                                && (x.IndexOrder == null || x.IndexOrder == 0)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblCallToTrough>> GetOrdersLedXiBao()
        {
            using (var dbContext = new XHTD_Entities())
            {
                List<string> listMachine = new List<string>() { 
                    MachineCode.CODE_MACHINE_1,
                    MachineCode.CODE_MACHINE_2,
                    MachineCode.CODE_MACHINE_3,
                    MachineCode.CODE_MACHINE_4,
                };

                var orders = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false
                                                && listMachine.Contains(x.Machine)
                                    )
                                    .OrderBy(x => x.IndexTrough)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblCallToTrough>> GetOrdersLedXiRoi()
        {
            using (var dbContext = new XHTD_Entities())
            {
                List<string> listMachine = new List<string>() {
                    MachineCode.CODE_MACHINE_9,
                    MachineCode.CODE_MACHINE_10,
                };

                var orders = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false
                                                && listMachine.Contains(x.Machine)
                                    )
                                    .OrderBy(x => x.IndexTrough)
                                    .ToListAsync();
                return orders;
            }
        }
    }
}
