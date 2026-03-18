namespace DMD.DOMAIN.Entities.UserProfile
{
    public class ClinicProfile : BaseEntity<int>
    {
        public string ClinicName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
    }
}
