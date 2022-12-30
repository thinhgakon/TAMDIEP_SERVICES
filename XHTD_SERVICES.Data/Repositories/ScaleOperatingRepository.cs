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
    public class ScaleOperatingRepository : BaseRepository<tblScaleOperating>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ScaleOperatingRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<bool> UpdateWhenConfirmEntrace(string scaleCode, string deliveryCode, string vehicle, string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == scaleCode);
                if (scaleInfo == null)
                {
                    return false;
                }

                scaleInfo.DeliveryCode = deliveryCode;
                scaleInfo.Vehicle = vehicle;
                scaleInfo.CardNo = cardNo;
                scaleInfo.ScaleIn = true;
                scaleInfo.ScaleOut = false;
                scaleInfo.IsScaling = true;
                scaleInfo.TimeIn = DateTime.Now;
                scaleInfo.UpdateDay = DateTime.Now;

                await dbContext.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> UpdateWhenConfirmExit(string scaleCode, string deliveryCode, string vehicle, string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == scaleCode);
                if (scaleInfo == null)
                {
                    return false;
                }

                scaleInfo.DeliveryCode = deliveryCode;
                scaleInfo.Vehicle = vehicle;
                scaleInfo.CardNo = cardNo;
                scaleInfo.ScaleIn = false;
                scaleInfo.ScaleOut = true;
                scaleInfo.IsScaling = true;
                scaleInfo.TimeIn = DateTime.Now;
                scaleInfo.UpdateDay = DateTime.Now;

                await dbContext.SaveChangesAsync();
                return true;
            }
        }

        public tblScaleOperating GetDetail(string scaleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var item = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == scaleCode);
                return item;
            }
        }

        public async Task<bool> ReleaseScale(string scaleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == scaleCode);
                if (scaleInfo == null)
                {
                    return false;
                }

                scaleInfo.DeliveryCode = "";
                scaleInfo.Vehicle = "";
                scaleInfo.CardNo = "";
                scaleInfo.ScaleIn = false;
                scaleInfo.ScaleOut = false;
                scaleInfo.IsScaling = false;
                scaleInfo.TimeIn = null;
                scaleInfo.UpdateDay = DateTime.Now;

                await dbContext.SaveChangesAsync();
                return true;
            }
        }
    }
}
