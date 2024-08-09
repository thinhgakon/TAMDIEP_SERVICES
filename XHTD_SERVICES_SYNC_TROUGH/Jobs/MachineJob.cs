using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    public class MachineJob : IJob
    {
        private readonly MachineRepository _machineRepository;

        public MachineJob(MachineRepository machineRepository)
        {
            _machineRepository = machineRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
               
            });
        }

        private async Task MachineJobProcess()
        {
            var machines = await _machineRepository.GetPendingMachine();

            if (machines == null || machines.Count == 0) return;

            foreach (var machine in machines)
            {
                if(machine.StartStatus == "PENDING")
                {
                    // call start to PLC

                    bool startResult = true;

                    if(startResult == true)
                    {
                        machine.StartStatus = "ON";
                        machine.StopStatus = "OFF";
                    }
                }

                if(machine.StopStatus == "PENDING")
                {
                    // call stop to PLC

                    bool stopResult = true;

                    if (stopResult == true)
                    {
                        machine.StartStatus = "OFF";
                        machine.StopStatus = "ON";
                    }
                }

                await _machineRepository.UpdateMachine(machine);
            }
        }
    }
}
