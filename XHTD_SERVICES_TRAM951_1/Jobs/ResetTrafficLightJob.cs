﻿using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device;
using XHTD_SERVICES_TRAM951_1.Devices;
using XHTD_SERVICES_TRAM951_1.Business;

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    [DisallowConcurrentExecution]
    public class ResetTrafficLightJob : IJob
    {
        protected readonly TCPTrafficLight _trafficLight;
        protected readonly Logger _logger;

        private tblCategoriesDevice trafficLight;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_1_DGT_IN;
        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_1_DGT_OUT;

        protected readonly string SCALE_CODE = ScaleCode.CODE_SCALE_1;

        public ResetTrafficLightJob(TCPTrafficLight trafficLight, Logger logger)
        {
            _trafficLight = trafficLight;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                //_logger.LogInfo("Start tramcan2 reset traffic light service");
                //_logger.LogInfo("----------------------------");

                TrafficLightProcess();
            });
        }

        public void TrafficLightProcess()
        {
            try
            {
                if (Program.scaleValuesForResetLight == null || Program.scaleValuesForResetLight.Count == 0)
                {
                    if (Program.IsFirstTimeResetTrafficLight)
                    {
                        Program.IsFirstTimeResetTrafficLight = false;

                        _logger.LogInfo("Reset traffic light - Scale 951 - 1");
                        TurnOffTrafficLight();
                    }
                    else
                    {
                        //log.Info("Khong co xe dang can => return");
                    }

                    return;
                }
                else
                {
                    Program.IsFirstTimeResetTrafficLight = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"RESET DGT ERROR: {ex.Message}");
            }
        }

        public void TurnOffTrafficLight()
        {
            _logger.LogInfo($@"Tắt đèn chiều vào");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOffTrafficLight(SCALE_DGT_IN_CODE))
            {
                _logger.LogInfo($@"Tắt thành công");
            }
            else
            {
                _logger.LogInfo($@"Tắt thất bại");
            }

            Thread.Sleep(500);

            _logger.LogInfo($@"Tắt đèn chiều ra");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOffTrafficLight(SCALE_DGT_OUT_CODE))
            {
                _logger.LogInfo($@"Tắt thành công");
            }
            else
            {
                _logger.LogInfo($@"Tắt thất bại");
            }
        }
    }
}
