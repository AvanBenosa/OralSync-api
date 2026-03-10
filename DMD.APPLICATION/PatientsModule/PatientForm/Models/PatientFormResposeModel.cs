using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientForm.Models
{
    public class PatientFormResposeModel
    {
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public int TotalCount { get; set; }
        public List<PatientFormModel> Items { get; set; }
    }
}
