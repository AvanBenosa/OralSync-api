namespace DMD.SERVICES.Email.Models
{
    public class PatientEmailJobRequest
    {
        public int PatientId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsBodyHtml { get; set; }
        public List<PatientEmailAttachmentJobRequest> Attachments { get; set; } = new();
    }
}
