using DMD.DOMAIN.Enums;

namespace DMD.APPLICATION.PatientsModule.PatientDentalChart.Models
{
    public class PatientDentalChartImageModel
    {
        public string Id { get; set; } = string.Empty;
        public string PatientTeethId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public FileType? FileType { get; set; }
        public string FileMediaType { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public int? DisplayOrder { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
