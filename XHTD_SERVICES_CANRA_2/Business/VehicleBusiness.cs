using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device;

namespace XHTD_SERVICES_CANRA_2.Business
{
    public class VehicleBusiness
    {
        protected readonly LongVehicleRepository _longVehicleRepository;

        public VehicleBusiness(
            LongVehicleRepository longVehicleRepository
            )
        {
            _longVehicleRepository = longVehicleRepository;
        }

        public async Task<bool> IsLongVehicle(string vehicleCode)
        {
            return await _longVehicleRepository.CheckExist(vehicleCode);
        }
    }
}
