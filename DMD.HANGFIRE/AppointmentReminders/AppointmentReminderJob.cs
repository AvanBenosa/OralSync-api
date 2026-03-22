using DMD.DOMAIN.Enums.Appointment;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.Sms;
using DMD.SERVICES.Sms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMD.HANGFIRE.AppointmentReminders
{
    public class AppointmentReminderJob : IAppointmentReminderJob
    {
        private readonly DmdDbContext dbContext;
        private readonly ISmsService smsService;
        private readonly AppointmentReminderSettings settings;
        private readonly ILogger<AppointmentReminderJob> logger;

        public AppointmentReminderJob(
            DmdDbContext dbContext,
            ISmsService smsService,
            IOptions<AppointmentReminderSettings> settings,
            ILogger<AppointmentReminderJob> logger)
        {
            this.dbContext = dbContext;
            this.smsService = smsService;
            this.settings = settings.Value;
            this.logger = logger;
        }

        public Task SendEveningBeforeAppointmentRemindersAsync(CancellationToken cancellationToken = default)
        {
            return SendAppointmentRemindersAsync(ReminderSchedule.EveningBefore, cancellationToken);
        }

        public Task SendMorningOfAppointmentRemindersAsync(CancellationToken cancellationToken = default)
        {
            return SendAppointmentRemindersAsync(ReminderSchedule.MorningOf, cancellationToken);
        }

        private async Task SendAppointmentRemindersAsync(
            ReminderSchedule schedule,
            CancellationToken cancellationToken = default)
        {
            if (!settings.IsEnabled)
            {
                logger.LogInformation("Appointment reminder job is disabled. Skipping {Schedule} run.", schedule);
                return;
            }

            var timeZone = AppointmentReminderTimeZoneResolver.Resolve(settings.TimeZoneId);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var queryWindow = BuildQueryWindow(schedule, localNow);

            var appointments = await (
                from appointment in dbContext.AppointmentRequests
                join patient in dbContext.PatientInfos on appointment.PatientInfoId equals patient.Id
                join clinic in dbContext.ClinicProfiles on patient.ClinicProfileId equals clinic.Id
                where appointment.Status == AppointmentStatus.Scheduled
                      && appointment.AppointmentDateFrom >= queryWindow.Start
                      && appointment.AppointmentDateFrom < queryWindow.End
                      && appointment.AppointmentDateFrom >= localNow
                      && appointment.SmsReminderSentForDate == queryWindow.ExpectedSmsReminderSentForDate
                orderby appointment.AppointmentDateFrom
                select new ReminderCandidate
                {
                    Appointment = appointment,
                    PatientId = patient.Id,
                    PatientFirstName = patient.FirstName,
                    PatientLastName = patient.LastName,
                    ContactNumber = patient.ContactNumber,
                    AppointmentDateFrom = appointment.AppointmentDateFrom,
                    ClinicName = clinic.ClinicName,
                    ClinicContactNumber = clinic.ContactNumber
                })
                .ToListAsync(cancellationToken);

            logger.LogInformation(
                "Appointment reminder job found {AppointmentCount} candidate appointments for {Schedule} on {ReminderDate}.",
                appointments.Count,
                schedule,
                localNow.Date.ToString("yyyy-MM-dd"));

            foreach (var candidate in appointments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var patientLogName = BuildPatientLogName(candidate.PatientFirstName, candidate.PatientLastName);

                if (string.IsNullOrWhiteSpace(candidate.ContactNumber))
                {
                    logger.LogWarning(
                        "Skipping {Schedule} appointment reminder for AppointmentId {AppointmentId}, PatientId {PatientId}, PatientName {PatientName} because the patient has no contact number.",
                        schedule,
                        candidate.Appointment.Id,
                        candidate.PatientId,
                        patientLogName);
                    continue;
                }

                try
                {
                    logger.LogInformation(
                        "Sending {Schedule} appointment reminder SMS to PatientName {PatientName}, RecipientNumber {RecipientNumber}, AppointmentId {AppointmentId}, AppointmentTime {AppointmentTime}.",
                        schedule,
                        patientLogName,
                        candidate.ContactNumber.Trim(),
                        candidate.Appointment.Id,
                        candidate.AppointmentDateFrom.ToString("yyyy-MM-dd hh:mm tt"));

                    await smsService.SendAsync(
                        new PatientSmsJobRequest
                        {
                            PatientId = candidate.PatientId,
                            RecipientNumber = candidate.ContactNumber.Trim(),
                            Message = BuildReminderMessage(candidate, schedule),
                            SenderName = candidate.ClinicName?.Trim() ?? string.Empty,
                            UsePriority = settings.UsePrioritySms
                        },
                        cancellationToken);

                    candidate.Appointment.SmsReminderSentForDate = schedule switch
                    {
                        ReminderSchedule.EveningBefore => null,
                        ReminderSchedule.MorningOf => candidate.AppointmentDateFrom.Date,
                        _ => candidate.Appointment.SmsReminderSentForDate
                    };
                    await dbContext.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Sent {Schedule} appointment reminder SMS to PatientName {PatientName}, RecipientNumber {RecipientNumber}, AppointmentId {AppointmentId}, PatientId {PatientId}.",
                        schedule,
                        patientLogName,
                        candidate.ContactNumber.Trim(),
                        candidate.Appointment.Id,
                        candidate.PatientId);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to send {Schedule} appointment reminder SMS to PatientName {PatientName}, RecipientNumber {RecipientNumber}, AppointmentId {AppointmentId}, PatientId {PatientId}.",
                        schedule,
                        patientLogName,
                        candidate.ContactNumber.Trim(),
                        candidate.Appointment.Id,
                        candidate.PatientId);
                }
            }
        }

        private static QueryWindow BuildQueryWindow(ReminderSchedule schedule, DateTime localNow)
        {
            return schedule switch
            {
                ReminderSchedule.EveningBefore => new QueryWindow
                {
                    Start = localNow.Date.AddDays(1),
                    End = localNow.Date.AddDays(2),
                    ExpectedSmsReminderSentForDate = localNow.Date
                },
                ReminderSchedule.MorningOf => new QueryWindow
                {
                    Start = localNow.Date,
                    End = localNow.Date.AddDays(1),
                    ExpectedSmsReminderSentForDate = null
                },
                _ => throw new InvalidOperationException($"Unsupported reminder schedule '{schedule}'.")
            };
        }

        private static string BuildReminderMessage(ReminderCandidate candidate, ReminderSchedule schedule)
        {
            var clinicName = string.IsNullOrWhiteSpace(candidate.ClinicName)
                ? "your clinic"
                : candidate.ClinicName.Trim();

            var patientName = BuildPatientName(candidate.PatientFirstName, candidate.PatientLastName);
            var appointmentTime = candidate.AppointmentDateFrom.ToString("hh:mm tt");
            var clinicContactSuffix = string.IsNullOrWhiteSpace(candidate.ClinicContactNumber)
                ? string.Empty
                : $" If you need help, please contact us at {candidate.ClinicContactNumber.Trim()}.";

            var appointmentDayText = schedule == ReminderSchedule.EveningBefore
                ? "tomorrow"
                : "today";

            return $"Hello {patientName}, this is a reminder from {clinicName}. You have an appointment {appointmentDayText} at {appointmentTime}.{clinicContactSuffix}";
        }

        private static string BuildPatientName(string? firstName, string? lastName)
        {
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                return firstName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                return lastName.Trim();
            }

            return "Patient";
        }

        private static string BuildPatientLogName(string? firstName, string? lastName)
        {
            var parts = new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .ToArray();

            return parts.Length > 0
                ? string.Join(" ", parts)
                : "Patient";
        }

        private sealed class ReminderCandidate
        {
            public required DMD.DOMAIN.Entities.Appointment.AppointmentRequest Appointment { get; init; }
            public int PatientId { get; init; }
            public string? PatientFirstName { get; init; }
            public string? PatientLastName { get; init; }
            public string? ContactNumber { get; init; }
            public DateTime AppointmentDateFrom { get; init; }
            public string? ClinicName { get; init; }
            public string? ClinicContactNumber { get; init; }
        }

        private sealed class QueryWindow
        {
            public DateTime Start { get; init; }
            public DateTime End { get; init; }
            public DateTime? ExpectedSmsReminderSentForDate { get; init; }
        }

        private enum ReminderSchedule
        {
            EveningBefore,
            MorningOf
        }
    }
}
