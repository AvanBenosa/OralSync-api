using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.DOMAIN.Entities.Appointment
{
    public class AppointmentRemarks : BaseEntity<int>
    {
        public int AppointmentRequestId { get; set; }
        public string Remarks { get; set; }
    }
}
