using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device;

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    public class TrafficLightInJob : IJob
    {
        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;
        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;
        protected readonly TCPTrafficLight _trafficLight;
        protected readonly Logger _logger;

        private tblCategoriesDevice trafficLight;

        protected readonly string CAT_CODE = "951-1";
        protected readonly string DEVICE_CODE = "951-1.DGT-IN";

        public TrafficLightInJob(CategoriesDevicesRepository categoriesDevicesRepository, 
                                CategoriesDevicesLogRepository categoriesDevicesLogRepository,
                                TCPTrafficLight trafficLight, 
                                Logger logger)
        {
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _trafficLight = trafficLight;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                _logger.LogInfo("Start tramcan1 traffic light IN 951 - 1 service");
                _logger.LogInfo("----------------------------");

                await LoadDevicesInfo();

                TrafficLightProcess();
            });
        }

        public void TrafficLightProcess()
        {
            try
            {
                if (Program.scaleValues == null || Program.scaleValues.Count == 0)
                {
                    if (Program.IsFirstTimeResetTrafficLightIn)
                    {
                        Program.IsFirstTimeResetTrafficLightIn = false;

                        TurnOnTrafficLight();

                        _logger.LogInfo("Reset traffic light IN - Scale 951 - 1");
                    }
                    else
                    {
                        //log.Info("Khong co xe dang can => return");
                    }

                    return;
                }
                else
                {
                    Program.IsFirstTimeResetTrafficLightIn = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"ERROR: {ex.Message}");
            }
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices(CAT_CODE);

            trafficLight = devices.FirstOrDefault(x => x.Code == DEVICE_CODE);
        }

        public string GetTrafficLightIpAddress()
        {
            var ipAddress = "";

            ipAddress = trafficLight?.IpAddress;

            return ipAddress;
        }

        public void TurnOnTrafficLight()
        {
            _logger.LogInfo($"7. Bật đèn xanh");
            if (TurnOnGreenTrafficLight())
            {
                _logger.LogInfo($"7.2. Bật đèn xanh thành công");
            }
            else
            {
                _logger.LogInfo($"7.2. Bật đèn xanh thất bại");
            }

            Thread.Sleep(10000);

            _logger.LogInfo($"8. Bật đèn đỏ");
            if (TurnOnRedTrafficLight())
            {
                _logger.LogInfo($"8.2. Bật đèn đỏ thành công");
            }
            else
            {
                _logger.LogInfo($"8.2. Bật đèn đỏ thất bại");
            }
        }

        public bool TurnOnGreenTrafficLight()
        {
            var ipAddress = GetTrafficLightIpAddress();

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _logger.LogInfo($"7.1. IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight()
        {
            var ipAddress = GetTrafficLightIpAddress();

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _logger.LogInfo($"8.1. IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOffGreenOnRed();
        }
    }
}
