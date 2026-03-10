
using DMD.DOMAIN.Enums;
using DMD.DOMAIN.Enums.Appointment;

namespace DMD.DOMAIN.Entities.Appointment
{
    public class AppointmentRequest : BaseEntity<int>
    {
        public string PatientName { get;set;  }

        public int PatientId { get; set;  }
        public DateTime AppointmentDate { get; set; }
        public string ReasonForVisit { get; set; }
        public int DoctorId { get; set; }
        public string Dentist { get; set; }
        public AppointmentStatus Status { get; set; }
        public List<AppointmentRemarks> Remarks { get; set;  }
    }
}
