using log4net;
using Quartz;
using S7.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device.PLCS71200;
using XHTD_SERVICES_GATEWAY.Devices;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class CaptureInOutJob : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(CaptureInOutJob));

        protected readonly S71200Sensor _sensor;

        private const string IP_ADDRESS = "192.168.13.166";
        private const short RACK = 0;
        private const short SLOT = 1;
        private const string GATE_IN = "I0.4";


        private readonly string CAMERA_IP = "192.168.13.167";
        private readonly string CAMERA_USER_NAME = "admin";
        private readonly string CAMERA_PASSWORD = "tamdiep@35";
        private readonly string IMG_PATH = "C:\\IMAGE";
        private readonly int CAMERA_NUMBER = 2;

        private const bool DEFAULT_STATUS = false;

        private readonly AttachmentRepository _attachmentRepository;
        private readonly CheckInOutRepository _checkInOutRepository;
        protected readonly GatewayLogger _gatewayLogger;

        public CaptureInOutJob(AttachmentRepository attachmentRepository, CheckInOutRepository checkInOutRepository, GatewayLogger gatewayLogger)
        {
            var plc = new Plc(CpuType.S71200, IP_ADDRESS, RACK, SLOT);
            _sensor = new S71200Sensor(plc);
            this._attachmentRepository = attachmentRepository;
            _checkInOutRepository = checkInOutRepository;
            _gatewayLogger = gatewayLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                Capture();
            });
        }

        public void Capture()
        {
            if (Program.IsCapturing)
            {
                Console.WriteLine("Capturing...");
                return;
            }

            Program.IsCapturing = true;
            try
            {
                _sensor.Open();
                if (_sensor.IsConnected == false)
                {
                    Console.WriteLine("Can not connect sensor!");
                    _sensor.Close();
                    Program.IsCapturing = false;
                    return;
                }
                var status = _sensor.ReadInputPort(GATE_IN);

                if (status)
                {
                    Program.IsBarrierOpen = true;
                }
                else
                {
                    Program.IsBarrierOpen = false;
                }

                if (Program.IsBarrierOpen)
                {
                    Console.WriteLine("Barrier dang MO");
                    if (Program.IsFirstTimeChange)
                    {
                        Console.WriteLine("LAN DAU");

                        // Thực hiện nghiệp vụ chụp ảnh ở đây 
                        var img = new HikvisionStreamCamera().CaptureStream(CAMERA_IP, CAMERA_USER_NAME, CAMERA_PASSWORD, "CHECKIN", CAMERA_NUMBER, IMG_PATH);

                        if (string.IsNullOrEmpty(img))
                        {
                            Console.WriteLine($"Capture fail");
                            _sensor.Close();
                            Program.IsCapturing = false;
                            return;
                        }

                        var attachmentId = _attachmentRepository.Create(new XHTD_SERVICES.Data.Entities.tblAttachment()
                        {
                            Url = img,
                            Extension = "JPG",
                            Type = "CHECKIN",
                            Title = $"IN_{DateTime.Now:ddMMyyyy_HHmmss}"
                        });

                        if (attachmentId == 0)
                        {
                            Console.WriteLine($"Add attachment fail");
                            _sensor.Close();
                            Program.IsCapturing = false;
                            return;
                        }

                        string currentVehicle = null;

                        if (Program.LastTimeValidVehicle >= DateTime.Now.AddSeconds(-15))
                        {
                            currentVehicle = Program.CurrentVehicleInGateway;
                        }

                        _checkInOutRepository.Create(new XHTD_SERVICES.Data.Entities.tblCheckInOut()
                        {
                            AttactmentId = attachmentId,
                            CheckInTime = DateTime.Now,
                            CheckOutTime = null,
                            LogProcess = $"#CheckIn time {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}",
                            Vehicle = currentVehicle
                        });

                        Console.WriteLine("Capture success");

                        Program.IsFirstTimeChange = false;
                    }
                    else
                    {
                        Console.WriteLine("KHONG PHAI LAN DAU");
                    }
                }
                else
                {
                    Console.WriteLine("Barrier dang DONG");

                    Program.IsFirstTimeChange = true;
                }
            }
            catch (Exception ex)
            {
                _gatewayLogger.LogError($"Capture fail {ex.Message}");
                _gatewayLogger.LogError($"Capture fail {ex.StackTrace}");
            }
            Program.IsCapturing = false;
        }
    }
}
