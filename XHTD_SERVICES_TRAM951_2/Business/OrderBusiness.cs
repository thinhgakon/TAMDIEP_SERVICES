using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM951_2.Business
{
    public class OrderBusiness
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public OrderBusiness(
            StoreOrderOperatingRepository storeOrderOperatingRepository
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task<tblStoreOrderOperating> GetDetail(string deliveryCode)
        {
            return await _storeOrderOperatingRepository.GetDetail(deliveryCode);
        }
    }
}
