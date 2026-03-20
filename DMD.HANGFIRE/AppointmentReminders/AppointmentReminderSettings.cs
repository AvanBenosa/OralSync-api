namespace DMD.HANGFIRE.AppointmentReminders
{
    public class AppointmentReminderSettings
    {
        public const string SectionName = "AppointmentReminderSettings";

        public bool IsEnabled { get; set; } = true;
        public string MorningCronExpression { get; set; } = "0 6 * * *";
        public string EveningCronExpression { get; set; } = "0 18 * * *";
        public string TimeZoneId { get; set; } = "Asia/Manila";
        public bool UsePrioritySms { get; set; }
    }
}
