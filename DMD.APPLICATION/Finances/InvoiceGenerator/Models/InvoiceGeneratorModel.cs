namespace DMD.APPLICATION.Finances.InvoiceGenerator.Models
{
    public class InvoiceGeneratorModel
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Procedure { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public double AmountPaid { get; set; }
        public double Balance { get; set; }
    }
}
