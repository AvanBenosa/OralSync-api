namespace DMD.APPLICATION.AdminPortal.Models
{
    public class AdminClinicModel
    {
        public string Id { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        public string ValidityDate { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public bool IsDataPrivacyAccepted { get; set; }
        public bool IsContractPolicyAccepted { get; set; }
        public bool ForBetaTestingAccepted { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
    }
}
