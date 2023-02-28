﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM481.Business
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

        public async Task UpdateWeightOut(string deliveryCode, int weightOut)
        {
            await _storeOrderOperatingRepository.UpdateWeightOut(deliveryCode, weightOut);
        }
    }
}
