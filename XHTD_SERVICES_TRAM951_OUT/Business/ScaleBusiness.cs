using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device;

namespace XHTD_SERVICES_TRAM951_OUT.Business
{
    public class ScaleBusiness
    {
        protected readonly ScaleOperatingRepository _scaleOperatingRepository;

        public ScaleBusiness(
            ScaleOperatingRepository scaleOperatingRepository
            )
        {
            _scaleOperatingRepository = scaleOperatingRepository;
        }

        public async Task<bool> ReleaseScale(string scaleCode)
        {
            return await _scaleOperatingRepository.ReleaseScale(scaleCode);
        }
    }
}
