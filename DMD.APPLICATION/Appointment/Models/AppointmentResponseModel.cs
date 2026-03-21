using System.Collections.Generic;

namespace DMD.APPLICATION.Appointment.Models
{
    public class AppointmentResponseModel
    {
        public List<AppointmentModel> Items { get; set; } = new();
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public int TotalCount { get; set; }
        public int SummaryCount { get; set; }
        public bool HasDateFilter { get; set; }
    }
}
