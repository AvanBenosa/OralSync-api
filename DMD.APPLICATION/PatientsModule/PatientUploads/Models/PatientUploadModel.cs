using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientUploads.Models
{
    public class PatientUploadModel
    {
        public int PatientsInfoId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public FileType FileType { get; set; }
        public string FileMediaType { get; set; }
        public string FileExtension { get; set; }
        public string Remarks { get; set; }
    }
}
