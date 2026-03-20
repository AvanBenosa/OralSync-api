using DMD.DOMAIN.Enums;

namespace DMD.APPLICATION.PatientsModule.PatientDentalPhotos.Models
{
    public class PatientDentalPhotoModel
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
        public string PatientTeethId { get; set; } = string.Empty;
        public int ToothNumber { get; set; }
        public ToothCondition? Condition { get; set; }
        public string ToothRemarks { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int? FileType { get; set; }
        public string FileMediaType { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
