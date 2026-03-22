using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PublicRegistration.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Appointment;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Enums.Appointment;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.Email;
using DMD.SERVICES.Email.Models;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NJsonSchema.Annotations;
using System.Net.Mail;

namespace DMD.APPLICATION.PublicRegistration.Commands.CreatePatientAppointment
{
    [JsonSchema("CreatePublicPatientAppointmentCommand")]
    public class Command : IRequest<Response>
    {
        public string ClinicId { get; set; } = string.Empty;
        public string ExistingPatientId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string EmailVerificationCode { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime AppointmentDateFrom { get; set; }
        public DateTime AppointmentDateTo { get; set; }
        public string ReasonForVisit { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IEmailQueueService emailQueueService;
        private readonly ILogger<CommandHandler> logger;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            DmdDbContext dbContext,
            IEmailQueueService emailQueueService,
            ILogger<CommandHandler> logger,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.emailQueueService = emailQueueService;
            this.logger = logger;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ClinicId))
                    return new BadRequestResponse("Clinic id is required.");

                var isExistingPatientRegistration = !string.IsNullOrWhiteSpace(request.ExistingPatientId);

                if (string.IsNullOrWhiteSpace(request.ExistingPatientId) &&
                    (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName)))
                    return new BadRequestResponse("Patient first name and last name are required.");

                if (string.IsNullOrWhiteSpace(request.ExistingPatientId))
                {
                    if (string.IsNullOrWhiteSpace(request.EmailAddress))
                        return new BadRequestResponse("Email address is required for new patient registration.");

                    if (string.IsNullOrWhiteSpace(request.EmailVerificationCode))
                        return new BadRequestResponse("Email verification code is required for new patient registration.");
                }

                if (request.AppointmentDateFrom >= request.AppointmentDateTo)
                    return new BadRequestResponse("Appointment end time must be later than the start time.");

                var clinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.ClinicId,
                    ProtectedIdPurpose.Clinic);

                if (!clinicId.HasValue)
                    return new BadRequestResponse("Clinic was not found.");

                var clinic = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == clinicId.Value, cancellationToken);

                if (clinic == null)
                    return new BadRequestResponse("Clinic was not found.");

                if (clinic.IsLocked)
                    return new BadRequestResponse("This clinic is not accepting appointment registrations right now.");

                var hasConflict = await (
                    from appointment in dbContext.AppointmentRequests.AsNoTracking()
                    join patientInfo in dbContext.PatientInfos.AsNoTracking()
                        on appointment.PatientInfoId equals patientInfo.Id.ToString()
                    where patientInfo.ClinicProfileId == clinicId.Value
                          && appointment.Status != AppointmentStatus.Cancelled
                          && appointment.AppointmentDateFrom < request.AppointmentDateTo
                          && appointment.AppointmentDateTo > request.AppointmentDateFrom
                    select appointment.Id
                ).AnyAsync(cancellationToken);

                if (hasConflict)
                    return new BadRequestResponse("Appointment schedule conflicts with an existing appointment.");

                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                PatientInfo patient;
                DMD.DOMAIN.Entities.UserProfile.PublicAppointmentEmailVerification? emailVerification = null;

                if (!string.IsNullOrWhiteSpace(request.ExistingPatientId))
                {
                    var existingPatientId = await protectionProvider.DecryptIntIdAsync(
                        request.ExistingPatientId,
                        ProtectedIdPurpose.Patient);

                    var existingPatient = await dbContext.PatientInfos
                        .FirstOrDefaultAsync(
                            x => x.Id == existingPatientId && x.ClinicProfileId == clinicId.Value,
                            cancellationToken);

                    if (existingPatient == null)
                        return new BadRequestResponse("Selected patient does not exist in this clinic.");

                    patient = existingPatient;
                }
                else
                {
                    var verificationCode = request.EmailVerificationCode.Trim();
                    var emailAddress = request.EmailAddress.Trim();
                    emailVerification = await dbContext.PublicAppointmentEmailVerifications
                        .FirstOrDefaultAsync(
                            item =>
                                item.ClinicProfileId == clinicId.Value &&
                                item.EmailAddress == emailAddress &&
                                item.ConsumedAtUtc == null,
                            cancellationToken);

                    if (emailVerification == null
                        || emailVerification.ExpiresAtUtc < DateTime.UtcNow
                        || !string.Equals(emailVerification.Code, verificationCode, StringComparison.Ordinal))
                    {
                        return new BadRequestResponse("Email verification code is invalid or expired.");
                    }

                    var today = DateTime.Today;
                    var countToday = await dbContext.PatientInfos
                        .IgnoreQueryFilters()
                        .CountAsync(p => p.CreatedAt.Date == today, cancellationToken);

                    var patientNumber = $"DMD-{today:yyyyMMdd}-{(countToday + 1):D4}";

                    patient = new PatientInfo
                    {
                        ClinicProfileId = clinicId.Value,
                        PatientNumber = patientNumber,
                        FirstName = request.FirstName.Trim(),
                        LastName = request.LastName.Trim(),
                        MiddleName = request.MiddleName?.Trim() ?? string.Empty,
                        EmailAddress = request.EmailAddress?.Trim() ?? string.Empty,
                        BirthDate = request.BirthDate,
                        ContactNumber = request.ContactNumber?.Trim() ?? string.Empty,
                        ProfilePicture = string.Empty,
                        Address = string.Empty,
                        Occupation = string.Empty,
                        Religion = string.Empty
                    };

                    dbContext.PatientInfos.Add(patient);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var newAppointment = new AppointmentRequest
                {
                    PatientInfoId = patient.Id.ToString(),
                    AppointmentDateFrom = request.AppointmentDateFrom,
                    AppointmentDateTo = request.AppointmentDateTo,
                    ReasonForVisit = request.ReasonForVisit?.Trim() ?? string.Empty,
                    Status = AppointmentStatus.Pending,
                    Remarks = request.Remarks?.Trim() ?? string.Empty,
                    AppointmentType = AppointmentType.Online,
                };

                dbContext.AppointmentRequests.Add(newAppointment);
                if (emailVerification != null)
                {
                    emailVerification.ConsumedAtUtc = DateTime.UtcNow;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                await TryQueueClinicAppointmentNotificationAsync(
                    clinic,
                    patient,
                    newAppointment,
                    isExistingPatientRegistration,
                    cancellationToken);

                return new SuccessResponse<PublicPatientAppointmentRegistrationModel>(
                    new PublicPatientAppointmentRegistrationModel
                    {
                        ClinicId = request.ClinicId,
                        ClinicName = clinic.ClinicName,
                        PatientId = await protectionProvider.EncryptIntIdAsync(patient.Id, ProtectedIdPurpose.Patient),
                        PatientNumber = patient.PatientNumber,
                        AppointmentId = await protectionProvider.EncryptIntIdAsync(newAppointment.Id, ProtectedIdPurpose.Appointment),
                        AppointmentDateFrom = newAppointment.AppointmentDateFrom,
                        AppointmentDateTo = newAppointment.AppointmentDateTo,
                    });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
            finally
            {
                await dbContext.DisposeAsync();
            }
        }

        private async Task TryQueueClinicAppointmentNotificationAsync(
            DMD.DOMAIN.Entities.UserProfile.ClinicProfile clinic,
            PatientInfo patient,
            AppointmentRequest appointment,
            bool isExistingPatientRegistration,
            CancellationToken cancellationToken)
        {
            var recipientEmail = clinic.EmailAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return;
            }

            if (!IsValidEmail(recipientEmail))
            {
                logger.LogWarning(
                    "Skipping public appointment clinic notification because the clinic email is invalid. ClinicId: {ClinicId}, EmailAddress: {EmailAddress}",
                    clinic.Id,
                    recipientEmail);
                return;
            }

            try
            {
                await emailQueueService.QueueAsync(
                    new PatientEmailJobRequest
                    {
                        PatientId = patient.Id,
                        RecipientEmail = recipientEmail,
                        Subject = BuildClinicAppointmentNotificationSubject(clinic, patient),
                        Body = BuildClinicAppointmentNotificationBody(
                            clinic,
                            patient,
                            appointment,
                            isExistingPatientRegistration),
                        IsBodyHtml = false
                    },
                    cancellationToken);
            }
            catch (Exception error)
            {
                logger.LogError(
                    error,
                    "Unable to queue clinic notification email for public appointment. ClinicId: {ClinicId}, PatientId: {PatientId}, AppointmentId: {AppointmentId}",
                    clinic.Id,
                    patient.Id,
                    appointment.Id);
            }
        }

        private static string BuildClinicAppointmentNotificationSubject(
            DMD.DOMAIN.Entities.UserProfile.ClinicProfile clinic,
            PatientInfo patient)
        {
            var patientName = BuildPatientDisplayName(patient);
            return string.IsNullOrWhiteSpace(patientName)
                ? $"{clinic.ClinicName}: New public appointment request"
                : $"{clinic.ClinicName}: New public appointment request from {patientName}";
        }

        private static string BuildClinicAppointmentNotificationBody(
            DMD.DOMAIN.Entities.UserProfile.ClinicProfile clinic,
            PatientInfo patient,
            AppointmentRequest appointment,
            bool isExistingPatientRegistration)
        {
            var patientName = BuildPatientDisplayName(patient);

            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"A new public appointment request has been created for {clinic.ClinicName}.",
                    string.Empty,
                    $"Registration type: {(isExistingPatientRegistration ? "Existing patient" : "New patient")}",
                    $"Patient name: {patientName}",
                    $"Patient number: {patient.PatientNumber}",
                    $"Email address: {ValueOrFallback(patient.EmailAddress)}",
                    $"Contact number: {ValueOrFallback(patient.ContactNumber)}",
                    $"Appointment date from: {appointment.AppointmentDateFrom:MMM dd, yyyy hh:mm tt}",
                    $"Appointment date to: {appointment.AppointmentDateTo:MMM dd, yyyy hh:mm tt}",
                    $"Reason for visit: {ValueOrFallback(appointment.ReasonForVisit)}",
                    $"Remarks: {ValueOrFallback(appointment.Remarks)}",
                    $"Status: {appointment.Status}",
                    $"Appointment type: {appointment.AppointmentType}",
                    string.Empty,
                    "Please review the appointment request in the Appointment module."
                });
        }

        private static string BuildPatientDisplayName(PatientInfo patient)
        {
            var values = new[]
            {
                patient.FirstName?.Trim(),
                patient.MiddleName?.Trim(),
                patient.LastName?.Trim()
            };

            var fullName = string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
            return string.IsNullOrWhiteSpace(fullName) ? "Unknown patient" : fullName;
        }

        private static string ValueOrFallback(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
        }

        private static bool IsValidEmail(string emailAddress)
        {
            try
            {
                _ = new MailAddress(emailAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
