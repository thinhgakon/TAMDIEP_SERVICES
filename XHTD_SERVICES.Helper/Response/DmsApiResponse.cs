using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES.Helper.Response
{
    public class DmsApiResponse
    {
        public bool Status { get; set; }
        public object Data { get; set; }
        public MessageObject MessageObject { get; set; }
    }

    public class MessageObject
    {
        public string Code { get; set; }
        public string Language { get; set; }
        public string Message { get; set; }
        public string MessageDetail { get; set; }
        public string MessageType { get; set; } // S (Success), W (Warning), E (Error)
        public string LogId { get; set; }
        public MessageObject()
        {
            Code = string.Empty;
            Message = string.Empty;
            MessageDetail = string.Empty;
            MessageType = string.Empty;
            LogId = string.Empty;
        }
    }
}
