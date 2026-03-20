namespace DMD.SERVICES.Sms.Models
{
    public class PatientSmsJobRequest
    {
        public int PatientId { get; set; }
        public string RecipientNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool UsePriority { get; set; }
    }
}
