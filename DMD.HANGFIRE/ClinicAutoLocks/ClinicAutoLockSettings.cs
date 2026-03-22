namespace DMD.HANGFIRE.ClinicAutoLocks
{
    public class ClinicAutoLockSettings
    {
        public const string SectionName = "ClinicAutoLockSettings";

        public bool IsEnabled { get; set; } = true;
        public string CronExpression { get; set; } = "0 0 * * *";
        public string TimeZoneId { get; set; } = "Asia/Manila";
    }
}
