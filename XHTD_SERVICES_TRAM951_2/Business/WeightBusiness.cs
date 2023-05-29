using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM951_2.Business
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

        public async Task UpdateWeightIn(string deliveryCode, int weightIn)
        {
            await _storeOrderOperatingRepository.UpdateWeightIn(deliveryCode, weightIn);
        }

        public async Task UpdateWeightInByCardNo(string cardNo, int weightIn)
        {
            await _storeOrderOperatingRepository.UpdateWeightInByCardNo(cardNo, weightIn);
        }

        public async Task UpdateWeightInByVehicleCode(string vehicleCode, int weightIn)
        {
            await _storeOrderOperatingRepository.UpdateWeightInByVehicleCode(vehicleCode, weightIn);
        }

        public async Task UpdateWeightOut(string deliveryCode, int weightOut)
        {
            await _storeOrderOperatingRepository.UpdateWeightOut(deliveryCode, weightOut);
        }
    }
}
