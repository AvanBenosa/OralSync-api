namespace DMD.APPLICATION.PublicRegistration.Models
{
    public class PublicExistingPatientLookupModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
    }
}
