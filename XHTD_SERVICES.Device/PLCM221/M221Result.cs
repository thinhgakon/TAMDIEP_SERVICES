using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Device.PLCM221
{
    public enum M221Result
    {
        ACKNOWLEDGE = 5,
        BYTECOUNT = 304,
        CONNECT_ERROR = 200,
        CONNECT_TIMEOUT = 201,
        CRC = 302,
        DEMO_TIMEOUT = 1000,
        FUNCTION = 306,
        GATEWAY_DEVICE_FAILED = 11,
        GATEWAY_PATH_UNAVAILABLE = 10,
        ILLEGAL_DATA_ADDRESS = 2,
        ILLEGAL_DATA_VALUE = 3,
        ILLEGAL_FUNCTION = 1,
        ISCLOSED = 301,
        MEMORY_PARITY_ERROR = 8,
        NEGATIVE_ACKNOWLEDGE = 7,
        QUANTITY = 305,
        READ = 203,
        RESPONSE = 303,
        RESPONSE_TIMEOUT = 300,
        SLAVE_DEVICE_BUSY = 6,
        SLAVE_DEVICE_FAILURE = 4,
        SUCCESS = 0,
        TRANSACTIONID = 307,
        WRITE = 202
    }
}
