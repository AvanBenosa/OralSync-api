namespace DMD.SERVICES.Sms.Models
{
    public class SemaphoreSmsSettings
    {
        public const string SectionName = "SemaphoreSmsSettings";

        public bool IsEnabled { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.semaphore.co/api/v4/";
        public string MessagesEndpoint { get; set; } = "messages";
        public string PriorityEndpoint { get; set; } = "priority";
        public int TimeoutMilliseconds { get; set; } = 30000;
    }
}
