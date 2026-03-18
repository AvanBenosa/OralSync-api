namespace DMD.APPLICATION.ClinicProfiles.Models
{
    public class DataPrivacyStatusModel
    {
        public string ClinicId { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public bool IsLocked { get; set; }
    }
}
