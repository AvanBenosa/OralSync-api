using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.DOMAIN.Entities.Patients
{
    public class PatientMedicalHistory : BaseEntity<int>
    {
        public int PatientInfoId { get; set; }
        public bool Q1 { get; set; }
        public bool Q2 { get; set; }
        public bool Q3 { get; set; }
        public bool Q4 { get; set; }
        public bool Q5 { get; set; }
        public bool Q6 { get; set; }
        public bool Q7 { get; set; }
        public bool Q8 { get; set; }
        public bool Q9 { get; set; }
        public bool Q10 { get; set; }
        public bool Q11 { get; set; }
        public bool Q12 { get; set; }
        public bool Q13 { get; set; }
        public string Others { get; set; }  
        public string Remarks { get; set; }
    }
}
