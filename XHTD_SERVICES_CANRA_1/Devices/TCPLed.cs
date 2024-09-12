using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XHTD_SERVICES_CANRA_1.Devices
{
    public class TCPLed
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;

        private string DATA_CODE = "*[L1]ON[L2]OFF[!]";

        private const int COUNT_RETRY_CONNECT = 1;

        private string IpAddress { get; set; }

        static ASCIIEncoding encoding = new ASCIIEncoding();

        public void Connect(string ipAddress)
        {
            IpAddress = ipAddress;
        }

        public void SetDataCode(string dataCode)
        {
            DATA_CODE = dataCode;
        }

        public bool DisplayScreen()
        {
            var isSuccessed = false;
            int count = 0;

            while (!isSuccessed && count < COUNT_RETRY_CONNECT)
            {
                count++;
                try
                {
                    //_logger.Error($@"TCPLed: count={count}");
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
                    byte[] data1 = encoding.GetBytes($"{DATA_CODE}");

                    stream.Write(data1, 0, data1.Length);

                    // 3. receive 1
                    data1 = new byte[BUFFER_SIZE];
                    stream.Read(data1, 0, BUFFER_SIZE);

                    // 5. Close
                    stream.Close();
                    client.Close();

                    isSuccessed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    _logger.Error($@"Loi TCPLed count={count}: {ex.Message} === {ex.StackTrace} === {ex.InnerException}");

                    Thread.Sleep(1000);
                }
            }

            return isSuccessed;
        }
    }
}
