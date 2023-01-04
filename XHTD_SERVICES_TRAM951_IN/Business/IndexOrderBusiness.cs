using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM951_IN.Business
{
    public class IndexOrderBusiness
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public IndexOrderBusiness(
            StoreOrderOperatingRepository storeOrderOperatingRepository
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task SetIndexOrder(string cardNo)
        {
            await _storeOrderOperatingRepository.SetIndexOrder(cardNo);
        }
    }
}
