using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES_TRAM951_1.Models.Response;

namespace XHTD_SERVICES_TRAM951_1.Business
{
    public class ScaleApiLib
    {
        public DesicionScaleResponse ScaleIn(string deliveryCode, int weight)
        {
            var response = new DesicionScaleResponse();
            response.Code = "01";
            response.Message = "Cân vào thành công";

            return response;
        }

        public DesicionScaleResponse ScaleOut(string deliveryCode, int weight)
        {
            var response = new DesicionScaleResponse();
            response.Code = "01";
            response.Message = "Cân ra thành công";

            return response;
        }
    }
}
