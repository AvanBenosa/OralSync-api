using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.DOMAIN.Entities.Patients
{
    public class PatientEmergencyContact : BaseEntity<int>
    {
        public int PatientInfoId { get; set; }
        public string FullName { get; set;  }
        public string Address { get; set;  }
        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }    
        public Relationship Relationship { get; set; } 
    }
}
