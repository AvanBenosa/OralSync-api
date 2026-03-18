namespace DMD.APPLICATION.ClinicProfiles.Models
{
    public class ClinicProfileModel
    {
        public int Id { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
    }
}
