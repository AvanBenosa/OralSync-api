namespace DMD.APPLICATION.ClinicProfiles.Models
{
    public class ClinicProfileModel
    {
        public string Id { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string BannerImagePath { get; set; } = string.Empty;
        public string QrCodeValue { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public bool IsContractPolicyAccepted { get; set; }
        public bool ForBetaTestingAccepted { get; set; }
        public string OpeningTime { get; set; } = "09:00";
        public string ClosingTime { get; set; } = "18:00";
        public string LunchStartTime { get; set; } = "12:00";
        public string LunchEndTime { get; set; } = "13:00";
        public bool IsMondayOpen { get; set; } = true;
        public bool IsTuesdayOpen { get; set; } = true;
        public bool IsWednesdayOpen { get; set; } = true;
        public bool IsThursdayOpen { get; set; } = true;
        public bool IsFridayOpen { get; set; } = true;
        public bool IsSaturdayOpen { get; set; }
        public bool IsSundayOpen { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public string ValidityDate { get; set; } = string.Empty;
        public int PatientCount { get; set; }
        public int UploadedFileCount { get; set; }
        public int UserCount { get; set; }
    }
}
