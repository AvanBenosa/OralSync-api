using DMD.DOMAIN.Enums;

namespace DMD.DOMAIN.Entities.Patients
{
    public class PatientTeethImage : BaseEntity<int>
    {
        public int PatientTeethId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public string FileMediaType { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string Remarks { get; set; } = string.Empty;

        public PatientTeeth? PatientTeeth { get; set; }
    }
}
