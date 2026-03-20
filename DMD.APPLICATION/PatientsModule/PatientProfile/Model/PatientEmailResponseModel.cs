namespace DMD.APPLICATION.PatientsModule.PatientProfile.Model
{
    public class PatientEmailResponseModel
    {
        public bool Queued { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public int AttachmentCount { get; set; }
    }
}
