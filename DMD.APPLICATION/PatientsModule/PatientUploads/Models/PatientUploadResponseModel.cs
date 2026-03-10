using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientUploads.Models
{
    public class PatientUploadResponseModel
    {
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public int TotalCount { get; set; }
        public List<PatientUploadModel> Items { get; set; }
    }
}
