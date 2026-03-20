using DMD.APPLICATION.Appointment.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums.Appointment;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.Appointment.Commands.Update
{
    [JsonSchema("AppointmentUpdateCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
        public DateTime AppointmentDateFrom { get; set; }
        public DateTime AppointmentDateTo { get; set; }
        public string ReasonForVisit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(DmdDbContext dbContext, IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.AppointmentDateFrom >= request.AppointmentDateTo)
                    return new BadRequestResponse("Appointment end time must be later than the start time.");

                var appointmentId = await protectionProvider.DecryptIntIdAsync(request.Id, ProtectedIdPurpose.Appointment);
                var item = await dbContext.AppointmentRequests
                    .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

                if (item == null)
                    return new BadRequestResponse("Item may have been modified or removed.");

                var patientId = await protectionProvider.DecryptIntIdAsync(request.PatientInfoId, ProtectedIdPurpose.Patient);
                var patient = await dbContext.PatientInfos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);

                if (patient == null)
                    return new BadRequestResponse("Selected patient does not exist.");

                if (!Enum.TryParse(request.Status, true, out AppointmentStatus status))
                    status = AppointmentStatus.Scheduled;

                var hasConflict = await dbContext.AppointmentRequests
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.Id != appointmentId &&
                            x.Status != AppointmentStatus.Cancelled &&
                            x.AppointmentDateFrom < request.AppointmentDateTo &&
                            x.AppointmentDateTo > request.AppointmentDateFrom,
                        cancellationToken);

                if (hasConflict)
                    return new BadRequestResponse("Appointment schedule conflicts with an existing appointment.");

                var shouldResetSmsReminder =
                    item.AppointmentDateFrom.Date != request.AppointmentDateFrom.Date
                    || item.Status != status;

                item.PatientInfoId = patientId.ToString();
                item.AppointmentDateFrom = request.AppointmentDateFrom;
                item.AppointmentDateTo = request.AppointmentDateTo;
                item.ReasonForVisit = request.ReasonForVisit?.Trim() ?? string.Empty;
                item.Status = status;
                item.Remarks = request.Remarks?.Trim() ?? string.Empty;

                if (shouldResetSmsReminder)
                {
                    item.SmsReminderSentForDate = status == AppointmentStatus.Scheduled
                        ? ResolveInitialSmsReminderSentForDate(request.AppointmentDateFrom)
                        : null;
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var patientName = string.Join(" ", new[] { patient.FirstName, patient.MiddleName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()));

                var response = new AppointmentModel
                {
                    Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Appointment),
                    PatientInfoId = await protectionProvider.EncryptIntIdAsync(patientId, ProtectedIdPurpose.Patient),
                    AppointmentDateFrom = item.AppointmentDateFrom,
                    AppointmentDateTo = item.AppointmentDateTo,
                    ReasonForVisit = item.ReasonForVisit,
                    Status = item.Status.ToString(),
                    Remarks = item.Remarks,
                    AppointmentType = item.AppointmentType.ToString(),
                    PatientNumber = patient.PatientNumber ?? string.Empty,
                    PatientName = string.IsNullOrWhiteSpace(patient.LastName)
                        ? patientName
                        : string.IsNullOrWhiteSpace(patientName)
                            ? patient.LastName
                            : $"{patient.LastName}, {patientName}"
                };

                return new SuccessResponse<AppointmentModel>(response);
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

        private static DateTime? ResolveInitialSmsReminderSentForDate(DateTime appointmentDateFrom)
        {
            return DateTime.Now.Date != appointmentDateFrom.Date
                ? appointmentDateFrom.AddDays(-1).Date
                : null;
        }
    }
}
