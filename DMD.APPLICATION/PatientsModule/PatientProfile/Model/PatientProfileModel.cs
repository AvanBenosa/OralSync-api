using DMD.DOMAIN.Enums;

namespace DMD.APPLICATION.PatientsModule.PatientProfile.Model
{
    public class PatientProfileModel
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public Suffix Suffix { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public BloodTypes BloodType { get; set; }
        public string CivilStatus { get; set; }
        public PatientTag Tag { get; set; }
    }
}
