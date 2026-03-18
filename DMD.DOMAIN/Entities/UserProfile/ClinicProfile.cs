using DMD.DOMAIN.Entities.Patients;

namespace DMD.DOMAIN.Entities.UserProfile
{
    public class ClinicProfile : BaseEntity<int>
    {
        public string ClinicName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public string OpeningTime { get; set; } = "09:00";
        public string ClosingTime { get; set; } = "18:00";
        public string LunchStartTime { get; set; } = "12:00";
        public string LunchEndTime { get; set; } = "13:00";
        public List<string> WorkingDays { get; set; } = new() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
        public List<PatientInfo> Patients { get; set; } = new();
    }
}
