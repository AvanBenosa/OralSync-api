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

            var fromName = string.IsNullOrWhiteSpace(emailSettings.FromName)
                ? emailSettings.FromEmail
                : emailSettings.FromName;

            using var message = new MailMessage
            {
                From = new MailAddress(emailSettings.FromEmail, fromName),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = false
            };

            message.To.Add(request.RecipientEmail);

            var credentials = emailSettings.BuildCredentials();
            using var client = new SmtpClient(emailSettings.Host, emailSettings.Port)
            {
                EnableSsl = emailSettings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = emailSettings.UseDefaultCredentials,
                Timeout = emailSettings.TimeoutMilliseconds
            };

            if (credentials is not null)
            {
                client.Credentials = credentials;
            }

            logger.LogInformation(
                "Sending SMTP patient email to {RecipientEmail} using host {Host}:{Port}. SSL: {EnableSsl}, DefaultCredentials: {UseDefaultCredentials}",
                request.RecipientEmail,
                emailSettings.Host,
                emailSettings.Port,
                emailSettings.EnableSsl,
                emailSettings.UseDefaultCredentials);

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

            if (emailSettings.Port <= 0)
            {
                throw new InvalidOperationException("EmailSettings:Port must be greater than zero.");
            }

            if (emailSettings.TimeoutMilliseconds <= 0)
            {
                throw new InvalidOperationException("EmailSettings:TimeoutMilliseconds must be greater than zero.");
            }

            if (!emailSettings.UseDefaultCredentials && string.IsNullOrWhiteSpace(emailSettings.UserName))
            {
                throw new InvalidOperationException("EmailSettings:UserName is not configured.");
            }

            if (!emailSettings.UseDefaultCredentials && string.IsNullOrWhiteSpace(emailSettings.Password))
            {
                throw new InvalidOperationException("EmailSettings:Password is not configured.");
            }
        }
    }
}
