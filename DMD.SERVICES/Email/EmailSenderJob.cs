using DMD.SERVICES.Email.Models;
using Microsoft.Extensions.Logging;

namespace DMD.SERVICES.Email
{
    public class EmailSenderJob : IEmailSenderJob
    {
        private readonly IEmailService emailService;
        private readonly ILogger<EmailSenderJob> logger;

        public EmailSenderJob(IEmailService emailService, ILogger<EmailSenderJob> logger)
        {
            this.emailService = emailService;
            this.logger = logger;
        }

        public async Task SendAsync(PatientEmailJobRequest request, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Sending queued patient email. PatientId: {PatientId}, RecipientEmail: {RecipientEmail}",
                request.PatientId,
                request.RecipientEmail);

            await emailService.SendAsync(request, cancellationToken);
        }
    }
}
