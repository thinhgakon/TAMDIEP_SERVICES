using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM481.Business
{
    public class UnladenWeightBusiness
    {
        protected readonly VehicleRepository _vehicleRepository;
        protected readonly RfidRepository _rfidRepository;

        public UnladenWeightBusiness(
            VehicleRepository vehicleRepository,
            RfidRepository rfidRepository
            )
        {
            _vehicleRepository = vehicleRepository;
            _rfidRepository = rfidRepository;
        }

        public async Task UpdateUnladenWeight(string cardNo, int weight)
        {
            await _vehicleRepository.UpdateUnladenWeight(cardNo, weight);
        }
    }
}
