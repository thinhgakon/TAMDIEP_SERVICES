using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM951_1.Business
{
    public class StepBusiness
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public StepBusiness(
            StoreOrderOperatingRepository storeOrderOperatingRepository
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task UpdateOrderConfirm3(string deliveryCode)
        {
            await _storeOrderOperatingRepository.UpdateOrderConfirm3ByDeliveryCode(deliveryCode);
        }

        public async Task UpdateOrderConfirm3ByCardNo(string cardNo)
        {
            await _storeOrderOperatingRepository.UpdateOrderConfirm3ByCardNo(cardNo);
        }

        public async Task UpdateOrderConfirm3ByVehicleCode(string vehicleCode)
        {
            await _storeOrderOperatingRepository.UpdateOrderConfirm3ByVehicleCode(vehicleCode);
        }

        public async Task UpdateOrderConfirm7(string deliveryCode)
        {
            await _storeOrderOperatingRepository.UpdateOrderConfirm7ByDeliveryCode(deliveryCode);
        }
    }
}
