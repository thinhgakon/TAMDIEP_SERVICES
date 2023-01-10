using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM481.Business
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

        public async Task SetIndexOrder(string deliveryCode)
        {
            await _storeOrderOperatingRepository.SetIndexOrder(deliveryCode);
        }
    }
}
