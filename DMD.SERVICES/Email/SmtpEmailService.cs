using System.Net;
using System.Net.Mail;
using DMD.SERVICES.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMD.SERVICES.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings emailSettings;
        private readonly ILogger<SmtpEmailService> logger;

        public SmtpEmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<SmtpEmailService> logger)
        {
            this.emailSettings = emailSettings.Value;
            this.logger = logger;
        }

        public async Task SendAsync(PatientEmailJobRequest request, CancellationToken cancellationToken = default)
        {
            ValidateSettings();

            using var message = new MailMessage
            {
                From = new MailAddress(emailSettings.FromEmail, emailSettings.FromName),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = false
            };

            message.To.Add(request.RecipientEmail);

            using var client = new SmtpClient(emailSettings.Host, emailSettings.Port)
            {
                EnableSsl = emailSettings.EnableSsl,
                Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password)
            };

            logger.LogInformation(
                "Sending SMTP patient email to {RecipientEmail} using host {Host}:{Port}",
                request.RecipientEmail,
                emailSettings.Host,
                emailSettings.Port);

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(emailSettings.Host))
            {
                throw new InvalidOperationException("EmailSettings:Host is not configured.");
            }

            if (string.IsNullOrWhiteSpace(emailSettings.FromEmail))
            {
                throw new InvalidOperationException("EmailSettings:FromEmail is not configured.");
            }

            if (string.IsNullOrWhiteSpace(emailSettings.UserName))
            {
                throw new InvalidOperationException("EmailSettings:UserName is not configured.");
            }

            if (string.IsNullOrWhiteSpace(emailSettings.Password))
            {
                throw new InvalidOperationException("EmailSettings:Password is not configured.");
            }
        }
    }
}
