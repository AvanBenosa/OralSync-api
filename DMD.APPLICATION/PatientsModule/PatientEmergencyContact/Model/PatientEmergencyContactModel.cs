using DMD.DOMAIN.Enums;

namespace DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Model
{
    public class PatientEmergencyContactModel
    {
        public int PatientsInfoId { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }
        public Relationship Relationship { get; set; }
    }
}
