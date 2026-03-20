
using DMD.DOMAIN.Enums;
using DMD.DOMAIN.Enums.Appointment;

namespace DMD.DOMAIN.Entities.Appointment
{
    public class AppointmentRequest : BaseEntity<int>
    {
        public string PatientInfoId { get;set;  }
        public DateTime AppointmentDateFrom { get; set; }
        public DateTime AppointmentDateTo { get; set; }
        public DateTime? SmsReminderSentForDate { get; set; }
        public string ReasonForVisit { get; set; }
        public AppointmentStatus Status { get; set; }
        public string Remarks { get; set; }
        public AppointmentType AppointmentType { get;set;  }
    }
}
