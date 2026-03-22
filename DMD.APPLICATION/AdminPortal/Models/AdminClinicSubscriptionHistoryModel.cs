namespace DMD.APPLICATION.AdminPortal.Models
{
    public class AdminClinicSubscriptionHistoryModel
    {
        public string Id { get; set; } = string.Empty;
        public string ClinicId { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
    }
}
