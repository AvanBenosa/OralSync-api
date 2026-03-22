namespace DMD.APPLICATION.PatientsModule.PatientProgressNotes.Models
{
    public class PatientProgressNoteModel
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;
        public string AssignedDoctor { get; set; }
        public DateTime? Date { get; set; }
        public string Procedure { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        //Amount
        public double Balance { get; set; }
        public string Account { get; set; } = string.Empty;
        public double Amount { get; set; }
        public double Discount { get; set; }
        public double TotalAmountDue { get; set; }
        public double AmountPaid { get; set; }

        public string ClinicalFinding { get; set; }
        public string Assessment { get; set; }
        public int? ToothNumber { get; set; }

        //Treatment Plan
        public DateTime? NextVisit { get; set; }
    }
}
