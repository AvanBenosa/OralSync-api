namespace DMD.HANGFIRE.AppointmentReminders
{
    public interface IAppointmentReminderJob
    {
        Task SendEveningBeforeAppointmentRemindersAsync(CancellationToken cancellationToken = default);
        Task SendMorningOfAppointmentRemindersAsync(CancellationToken cancellationToken = default);
    }
}
