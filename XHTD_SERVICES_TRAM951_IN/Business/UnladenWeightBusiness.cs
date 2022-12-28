using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_TRAM951_IN.Business
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

        public async Task UpdateUnladenWeight(string vehicle, int weight)
        {
            var rfid = _rfidRepository.GetRfidbyVehicle(vehicle);

            if (rfid != null) { 
                await _vehicleRepository.UpdateUnladenWeight(rfid, weight);
            }
        }
    }
}
