using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.DOMAIN.Entities.Patients
{
    public class PatientOverview : BaseEntity<int>
    {
        public int PatientInfoId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Procedure { get; set; }
        public double Balance { get; set; }
        public double PaidAmount { get; set; }
        public double TotalAmount { get; set; }
        public string Remarks { get; set; }
    }
}
