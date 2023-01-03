using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM951_IN.Business
{
    public class WeightBusiness
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public WeightBusiness(
            StoreOrderOperatingRepository storeOrderOperatingRepository
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task UpdateWeightIn(string cardNo, int weightIn)
        {
            await _storeOrderOperatingRepository.UpdateWeightIn(cardNo, weightIn);
        }

        public async Task UpdateWeightOut(string cardNo, int weightIn)
        {
            await _storeOrderOperatingRepository.UpdateOrderExitTram951(cardNo, weightIn);
        }
    }
}
