namespace DMD.APPLICATION.Dashboard.Models
{
    public class DashboardResponseModel
    {
        public int TotalPatients { get; set; }
        public int PatientsToday { get; set; }
        public int ScheduledAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public double IncomeToday { get; set; }
        public double TotalIncomeMonthly { get; set; }
        public double TotalExpenseMonthly { get; set; }
        public List<DashboardPatientItemModel> LatestPatients { get; set; } = new();
        public bool AddPatients { get; set; }
        public bool AddAppointment { get; set; }
        public List<MonthlyIncomeModel>? MonthlyIncome { get; set; }
        public List<MonthlyRevenueModel>? MonthlyRevenue { get; set; }
        public List<DashboardAppointmentModel> NextDayAppointment { get; set; } = new();
        public List<DashboardAppointmentModel> TodayAppointment { get; set; } = new();
    }
}
