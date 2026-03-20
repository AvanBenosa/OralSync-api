using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientProfile.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using DMD.SERVICES.Sms;
using DMD.SERVICES.Sms.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientProfile.Commands.SendSms
{
    [JsonSchema("SendPatientSmsCommand")]
    public class Command : IRequest<Response>
    {
        public string PatientId { get; set; } = string.Empty;
        public string RecipientNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool UsePriority { get; set; }
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly ISmsQueueService smsQueueService;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            DmdDbContext dbContext,
            ISmsQueueService smsQueueService,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.smsQueueService = smsQueueService;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PatientId))
                {
                    return new BadRequestResponse("Patient ID is required.");
                }

                if (string.IsNullOrWhiteSpace(request.RecipientNumber))
                {
                    return new BadRequestResponse("Recipient number is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return new BadRequestResponse("SMS message is required.");
                }

                var patientId = await protectionProvider.DecryptIntIdAsync(
                    request.PatientId,
                    ProtectedIdPurpose.Patient);

                var patient = await dbContext.PatientInfos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);

                if (patient == null)
                {
                    return new BadRequestResponse("Patient not found.");
                }

                var clinicName = await dbContext.ClinicProfiles
                    .AsNoTracking()
                    .Where(x => x.Id == patient.ClinicProfileId)
                    .Select(x => x.ClinicName)
                    .FirstOrDefaultAsync(cancellationToken);

                var normalizedNumber = NormalizeSingleNumber(request.RecipientNumber);
                var senderName = string.IsNullOrWhiteSpace(request.SenderName)
                    ? clinicName?.Trim() ?? string.Empty
                    : request.SenderName.Trim();

                await smsQueueService.QueueAsync(
                    new PatientSmsJobRequest
                    {
                        PatientId = patient.Id,
                        RecipientNumber = normalizedNumber,
                        Message = request.Message.Trim(),
                        SenderName = senderName,
                        UsePriority = request.UsePriority
                    },
                    cancellationToken);

                return new SuccessResponse<PatientSmsResponseModel>(
                    new PatientSmsResponseModel
                    {
                        Queued = true,
                        RecipientNumber = normalizedNumber,
                        Message = request.Message.Trim(),
                        QueuedAt = DateTime.UtcNow
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

        private static string NormalizeSingleNumber(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                throw new InvalidOperationException("Recipient number contains no digits.");
            }

            if (digits.StartsWith("09") && digits.Length == 11)
            {
                return $"63{digits[1..]}";
            }

            if (digits.StartsWith("9") && digits.Length == 10)
            {
                return $"63{digits}";
            }

            if (digits.StartsWith("639") && digits.Length == 12)
            {
                return digits;
            }

            throw new InvalidOperationException(
                $"Recipient number '{value}' is not a supported Philippine mobile number.");
        }
    }
}
