namespace XHTD_SERVICES.Helper.Models.Request
{
    public class SendGatewayNotificationRequest
    {
        public string InOut { get; set; }
        public int Status { get; set; }
        public string CardNo { get; set; }
        public string Message { get; set; }

        public string Vehicle { get; set; }
    }
}
