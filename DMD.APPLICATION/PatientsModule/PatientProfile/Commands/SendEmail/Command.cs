using System.Net.Mail;
using DMD.APPLICATION.PatientsModule.PatientProfile.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.Email;
using DMD.SERVICES.Email.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientProfile.Commands.SendEmail
{
    [JsonSchema("SendPatientEmailCommand")]
    public class Command : IRequest<Response>
    {
        public int PatientId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IEmailQueueService emailQueueService;

        public CommandHandler(DmdDbContext dbContext, IEmailQueueService emailQueueService)
        {
            this.dbContext = dbContext;
            this.emailQueueService = emailQueueService;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.PatientId <= 0)
                {
                    return new BadRequestResponse("Patient ID is required.");
                }

                if (string.IsNullOrWhiteSpace(request.RecipientEmail))
                {
                    return new BadRequestResponse("Recipient email is required.");
                }

                if (!IsValidEmail(request.RecipientEmail))
                {
                    return new BadRequestResponse("Recipient email is invalid.");
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    return new BadRequestResponse("Email subject is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Body))
                {
                    return new BadRequestResponse("Email message is required.");
                }

                var patient = await dbContext.PatientInfos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken);

                if (patient == null)
                {
                    return new BadRequestResponse("Patient not found.");
                }

                await emailQueueService.QueueAsync(new PatientEmailJobRequest
                {
                    PatientId = patient.Id,
                    RecipientEmail = request.RecipientEmail.Trim(),
                    Subject = request.Subject.Trim(),
                    Body = request.Body.Trim()
                }, cancellationToken);

                return new SuccessResponse<PatientEmailResponseModel>(new PatientEmailResponseModel
                {
                    Queued = true,
                    RecipientEmail = request.RecipientEmail.Trim(),
                    Subject = request.Subject.Trim(),
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
