using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using log4net;
using System.Threading;

namespace XHTD_SERVICES.Device
{
    public class TCPTrafficLight
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;

        private const string ONGREENOFFRED = "*[L1]ON[L2]OFF[!]";
        private const string OFFGREENONRED = "*[L1]OFF[L2]ON[!]";
        private const string OFFGREENOFFRED = "*[L1]OFF[L2]OFF[!]";

        private const int COUNT_RETRY_CONNECT = 3;
        private const int COUNT_RETRY_CONNECT_OFF = 3;

        private string IpAddress { get; set; }

        static ASCIIEncoding encoding = new ASCIIEncoding();

        public void Connect(string ipAddress)
        {
            IpAddress = ipAddress;
        }

        public bool TurnOnGreenOffRed()
        {
            var isSuccessed = false;
            int count = 0;

            while (!isSuccessed && count < COUNT_RETRY_CONNECT)
            {
                count++;
                try
                {
                    _logger.Error($@"Bat den xanh: count={count}");
                    TcpClient client = new TcpClient();

                    // 1. connect
                    var isConnected = client.ConnectAsync($"{this.IpAddress}", PORT_NUMBER).Wait(3000);
                    if (!isConnected)
                    {
                        // connection failure
                        _logger.Error($@"Khong the connect count={count}");
                        continue;
                    }

                    //client.Connect($"{this.IpAddress}", PORT_NUMBER);
                    Stream stream = client.GetStream();

                    // 2. send 1
                    byte[] data1 = encoding.GetBytes($"{ONGREENOFFRED}");

                    stream.WriteAsync(data1, 0, data1.Length).Wait(3000);

                    // 5. Close
                    stream.Close();
                    client.Close();

                    isSuccessed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    _logger.Error($@"BAT DEN XANH ERROR count={count}: {ex.Message} === {ex.StackTrace} === {ex.InnerException}");
                    //return false;

                    Thread.Sleep(1000);
                }
            }

            return isSuccessed;
        }

        public bool TurnOffGreenOnRed()
        {
            var isSuccessed = false;
            int count = 0;

            while (!isSuccessed && count < COUNT_RETRY_CONNECT)
            {
                count++;
                try
                {
                    _logger.Error($@"Bat den do: count={count}");

                    TcpClient client = new TcpClient();

                    // 1. connect
                    var isConnected = client.ConnectAsync($"{this.IpAddress}", PORT_NUMBER).Wait(3000);
                    if (!isConnected)
                    {
                        // connection failure
                        _logger.Error($@"Khong the connect count={count}");
                        continue;
                    }

                    Stream stream = client.GetStream();

                    // 2. send 1
                    byte[] data1 = encoding.GetBytes($"{OFFGREENONRED}");

                    stream.WriteAsync(data1, 0, data1.Length).Wait(3000);

                    // 5. Close
                    stream.Close();
                    client.Close();

                    isSuccessed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    _logger.Error($@"BAT DEN DO ERROR count={count}: {ex.Message} === {ex.StackTrace} === {ex.InnerException}");

                    Thread.Sleep(1000);
                }
            }
            return isSuccessed;
        }

        public bool TurnOffGreenOffRed()
        {
            var isSuccessed = false;
            int count = 0;

            while (!isSuccessed && count < COUNT_RETRY_CONNECT_OFF)
            {
                count++;
                try
                {
                    _logger.Error($@"Tat den xanh do: count={count}");

                    TcpClient client = new TcpClient();

                    // 1. connect
                    var isConnected = client.ConnectAsync($"{this.IpAddress}", PORT_NUMBER).Wait(3000);
                    if (!isConnected)
                    {
                        // connection failure
                        _logger.Error($@"Khong the connect count={count}");
                        continue;
                    }

                    Stream stream = client.GetStream();

                    // 2. send 1
                    byte[] data1 = encoding.GetBytes($"{OFFGREENOFFRED}");

                    stream.WriteAsync(data1, 0, data1.Length).Wait(3000);

                    // 5. Close
                    stream.Close();
                    client.Close();

                    isSuccessed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    _logger.Error($@"TAT DEN DO ERROR count={count}: {ex.Message} === {ex.StackTrace} === {ex.InnerException}");

                    Thread.Sleep(1000);
                }
            }
            return isSuccessed;
        }
    }
}
