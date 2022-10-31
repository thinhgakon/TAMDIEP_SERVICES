using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES.Data.Repositories
{
    public abstract class BaseRepository<T> where T : class
    {
        protected readonly XHTD_Entities _appDbContext;

        protected BaseRepository(XHTD_Entities appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<T> FindByIdAsync(int id)
        {
            return await _appDbContext.Set<T>().FindAsync(id);
        }
    }
}
