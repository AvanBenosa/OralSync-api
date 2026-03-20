namespace DMD.APPLICATION.PatientsModule.PatientProfile.Model
{
    public class PatientSmsResponseModel
    {
        public bool Queued { get; set; }
        public string RecipientNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
    }
}
