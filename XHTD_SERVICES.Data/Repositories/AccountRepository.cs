using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES.Data.Repositories
{
    public class AccountRepository : BaseRepository <tblAccount>
    {
        public AccountRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public  List<tblAccount> GetListAccount()
        {
            var nhanviens = _appDbContext.tblAccounts.ToList();
            return nhanviens;
        }

        public async Task<tblAccount> GetDetailAccount()
        {
            return await FindByIdAsync(1);
        }
    }
}
