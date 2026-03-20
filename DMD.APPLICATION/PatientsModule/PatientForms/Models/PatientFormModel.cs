namespace DMD.APPLICATION.PatientsModule.PatientForms.Models
{
    public class PatientFormModel
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
        public string TemplateFormId { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public string ReportTemplate { get; set; } = string.Empty;
        public string AssignedDoctor { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
