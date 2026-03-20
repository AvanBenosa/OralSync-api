using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.DOMAIN.Entities.Patients
{
    public class PatientForm : BaseEntity<int>
    {
        public int PatientInfoId { get; set; }
        public DateTime? Date { get; set; }
        public string Remarks { get; set;  }
        public string AssignedDoctor { get; set; }

        public int FormTemplateId { get; set; }
    }
}
