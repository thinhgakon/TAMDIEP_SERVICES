using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using XHTD_SERVICES_XB_TROUGH_5.Models.Response;

namespace XHTD_SERVICES_XB_TROUGH_5.Business
{
    public class MachineApiLib
    {
        public MachineResponse StartMachine(MachineControlRequest requestData)
        {
            var startMachineResponse = HttpRequest.StartMachine(requestData);
            var startMachineResponseContent = startMachineResponse.Content;
            var response = JsonConvert.DeserializeObject<MachineResponse>(startMachineResponseContent);

            var resultResponse = new MachineResponse
            {
                Status = false,
                MessageObject = new MessageObject
                {
                    Code = "0104"
                }
            };
            resultResponse.Status = response.Status;
            resultResponse.MessageObject.Code = response.MessageObject.Code;

            return resultResponse;
        }

        public MachineResponse StopMachine(MachineControlRequest requestData)
        {
            var stopMachineResponse = HttpRequest.StopMachine(requestData);
            var stopMachineResponseContent = stopMachineResponse.Content;
            var response = JsonConvert.DeserializeObject<MachineResponse>(stopMachineResponseContent);

            var resultResponse = new MachineResponse
            {
                Status = false,
                MessageObject = new MessageObject
                {
                    Code = "0104"
                }
            };
            resultResponse.Status = response.Status;
            resultResponse.MessageObject.Code = response.MessageObject.Code;

            return resultResponse;
        }
    }
}
