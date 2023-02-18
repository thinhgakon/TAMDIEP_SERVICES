using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device;
using XHTD_SERVICES_TRAM951_1.Models.Response;

namespace XHTD_SERVICES_TRAM951_1.Business
{
    public class DesicionScaleBusiness
    {
        protected readonly ScaleOperatingRepository _scaleOperatingRepository;

        public DesicionScaleBusiness(
            ScaleOperatingRepository scaleOperatingRepository
            )
        {
            _scaleOperatingRepository = scaleOperatingRepository;
        }

        public DesicionScaleResponse MakeDecisionScaleIn(string deliveryCode, int weight)
        {
            var response = DIBootstrapper.Init().Resolve<ScaleApiLib>().ScaleIn(deliveryCode, weight);

            return response;
        }

        public DesicionScaleResponse MakeDecisionScaleOut(string deliveryCode, int weight)
        {
            var response = DIBootstrapper.Init().Resolve<ScaleApiLib>().ScaleOut(deliveryCode, weight);

            return response;
        }
    }
}
