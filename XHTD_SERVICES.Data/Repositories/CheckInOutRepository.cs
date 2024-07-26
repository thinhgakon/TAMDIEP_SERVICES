using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES.Data.Repositories
{
    public class CheckInOutRepository : BaseRepository<tblCheckInOut>
    {
        public CheckInOutRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public bool Create(tblCheckInOut checkInOut)
        {
            try
            {
                _appDbContext.tblCheckInOuts.Add(checkInOut);
                _appDbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Add attachment fail: {ex.Message}");
                return false;
            }
        }
    }
}
