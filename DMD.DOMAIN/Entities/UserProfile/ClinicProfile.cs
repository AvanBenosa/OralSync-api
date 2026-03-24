using DMD.DOMAIN.Entities.Buildups;
using DMD.DOMAIN.Entities.FInances;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Enums;

namespace DMD.DOMAIN.Entities.UserProfile
{
    public class ClinicProfile : BaseEntity<int>
    {
        public string ClinicName { get; set; } = string.Empty;
        public string BannerImagePath { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public bool IsContractPolicyAccepted { get; set; }
        public bool ForBetaTestingAccepted { get; set; }
        public bool IsLocked { get; set;  }
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

        public DateTime ValidityDate { get; set; }
        public SubscriptionType Subsciption { get; set; }
        public List<PatientInfo> Patients { get; set; } = new();

        public List<ClinicExpenses>Expenses { get; set; } = new();
        public List<FormTemplate>FormTemplates { get; set; } = new();
        public List<DentalInventory>Inventories { get; set; } = new();
        public List<ClinicSubsciptionHistory> SubsciptionHistories { get; set; } = new();
    }
}
