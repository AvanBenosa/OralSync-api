using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.DOMAIN.Entities.Patients
{
    public class PatientTeeth : BaseEntity<int>
    {
        public int PatientInfoId { get; set; }

        // Tooth number (1 - 32)
        public int ToothNumber { get; set; }

        public ToothCondition Condition { get; set; }

        // Dentist remarks
        public string Remarks { get; set; }

        public PatientTeethSurface TeethSurface { get; set; }

    }
}
