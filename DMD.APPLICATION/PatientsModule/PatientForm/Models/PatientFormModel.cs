using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientForm.Models
{
    public class PatientFormModel
    {
        public int Id { get; set; }
        public int PatientInfoId { get; set; }
        public DateTime? Date { get; set; }
        public string Remarks { get; set; }
        public PatientFormTypes FormType { get; set; }
    }
}
