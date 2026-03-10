using DMD.DOMAIN.Enums;

namespace DMD.DOMAIN.Entities.Patients
{
    public class PatientUploads : BaseEntity<int>
    {
        public int PatientInfoId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public FileType FileType { get; set; }
        public string FileMediaType { get; set; }
        public string FileExtension { get; set; }
        public string Remarks { get; set; }
    }
}
