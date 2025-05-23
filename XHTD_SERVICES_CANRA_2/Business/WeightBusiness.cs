﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Response;
using XHTD_SERVICES_CANRA_2.Models.Response;

namespace XHTD_SERVICES_CANRA_2.Business
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

        public async Task<DesicionScaleResponse> UpdateLotNumber(string deliveryCode)
        {
            var lotNumber = await _storeOrderOperatingRepository.UpdateLotNumber(deliveryCode);

            // Nếu cập nhật số lô thành công thì gọi api ERP cập nhật lại số lô
            if (!string.IsNullOrEmpty(lotNumber))
            {
                var updateResponse = HttpRequest.UpdateLotNumber(deliveryCode, lotNumber);
                if (updateResponse.StatusDescription.Equals("Unauthorized"))
                {
                    var unauthorizedResponse = new DesicionScaleResponse();
                    unauthorizedResponse.Code = "02";
                    unauthorizedResponse.Message = "Xác thực API cân WebSale không thành công";
                    return unauthorizedResponse;
                }

                var updateResponseContent = updateResponse.Content;
                var response = JsonConvert.DeserializeObject<DesicionScaleResponse>(updateResponseContent);
                return response;
            }

            var resultResponse = new DesicionScaleResponse
            {
                Code = "02",
                Message = "Cập nhật số lô thất bại, không tìm thấy số lô hợp lệ"
            };

            return resultResponse;
        }

        public async Task<DmsApiResponse> InvoiceXHTD(string deliveryCode)
        {
            var updateResponse = HttpRequest.SendInvoiceXHTD(deliveryCode);
            var updateResponseContent = updateResponse.Content;
            var response = JsonConvert.DeserializeObject<DmsApiResponse>(updateResponseContent);
            return response;
        }
    }
}
