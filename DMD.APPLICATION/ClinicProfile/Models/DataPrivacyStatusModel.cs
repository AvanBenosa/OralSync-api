namespace DMD.APPLICATION.ClinicProfiles.Models
{
    public class DataPrivacyStatusModel
    {
        public int ClinicId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public bool IsLocked { get; set; }
    }
}
