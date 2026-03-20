using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientProfile.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.Email;
using DMD.SERVICES.Email.Models;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientProfile.Commands.SendEmail
{
    [JsonSchema("SendPatientEmailCommand")]
    public class Command : IRequest<Response>
    {
        public string PatientId { get; set; } = string.Empty;
        public string TemplateFormId { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsBodyHtml { get; set; }
        public List<PatientEmailAttachmentModel> Attachments { get; set; } = new();
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IEmailQueueService emailQueueService;
        private readonly IProtectionProvider protectionProvider;
        private static readonly HashSet<string> AllowedAttachmentExtensions = new(
            new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt" },
            StringComparer.OrdinalIgnoreCase);
        private const int MaxAttachmentCount = 5;
        private const int MaxAttachmentSizeBytes = 10 * 1024 * 1024;

        public CommandHandler(
            DmdDbContext dbContext,
            IEmailQueueService emailQueueService,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.emailQueueService = emailQueueService;
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

                if (!HasMeaningfulContent(request.Body))
                {
                    return new BadRequestResponse("Email message is required.");
                }

                var requestedAttachments = request.Attachments ?? new List<PatientEmailAttachmentModel>();

                if (requestedAttachments.Count > MaxAttachmentCount)
                {
                    return new BadRequestResponse($"You can attach up to {MaxAttachmentCount} files only.");
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

                var attachments = new List<PatientEmailAttachmentJobRequest>();

                foreach (var attachment in requestedAttachments)
                {
                    if (string.IsNullOrWhiteSpace(attachment.FileName))
                    {
                        return new BadRequestResponse("Attachment file name is required.");
                    }

                    if (string.IsNullOrWhiteSpace(attachment.Base64Content))
                    {
                        return new BadRequestResponse($"Attachment content is required for \"{attachment.FileName}\".");
                    }

                    var extension = Path.GetExtension(attachment.FileName.Trim());
                    if (string.IsNullOrWhiteSpace(extension) || !AllowedAttachmentExtensions.Contains(extension))
                    {
                        return new BadRequestResponse($"\"{attachment.FileName}\" is not a supported attachment type.");
                    }

                    byte[] content;
                    try
                    {
                        content = Convert.FromBase64String(attachment.Base64Content.Trim());
                    }
                    catch (FormatException)
                    {
                        return new BadRequestResponse($"Attachment \"{attachment.FileName}\" is not a valid base64 file.");
                    }

                    if (content.Length == 0)
                    {
                        return new BadRequestResponse($"Attachment \"{attachment.FileName}\" is empty.");
                    }

                    if (content.Length > MaxAttachmentSizeBytes)
                    {
                        return new BadRequestResponse($"\"{attachment.FileName}\" exceeds the 10 MB attachment limit.");
                    }

                    attachments.Add(new PatientEmailAttachmentJobRequest
                    {
                        FileName = attachment.FileName.Trim(),
                        ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                            ? "application/octet-stream"
                            : attachment.ContentType.Trim(),
                        Content = content
                    });
                }

                await emailQueueService.QueueAsync(new PatientEmailJobRequest
                {
                    PatientId = patient.Id,
                    RecipientEmail = request.RecipientEmail.Trim(),
                    Subject = request.Subject.Trim(),
                    Body = request.Body.Trim(),
                    IsBodyHtml = request.IsBodyHtml,
                    Attachments = attachments
                }, cancellationToken);

                return new SuccessResponse<PatientEmailResponseModel>(new PatientEmailResponseModel
                {
                    Queued = true,
                    RecipientEmail = request.RecipientEmail.Trim(),
                    Subject = request.Subject.Trim(),
                    QueuedAt = DateTime.UtcNow,
                    AttachmentCount = attachments.Count
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

        private static bool HasMeaningfulContent(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            var withoutStyles = Regex.Replace(body, "<style[\\s\\S]*?</style>", " ", RegexOptions.IgnoreCase);
            var withoutScripts = Regex.Replace(withoutStyles, "<script[\\s\\S]*?</script>", " ", RegexOptions.IgnoreCase);
            var withoutTags = Regex.Replace(withoutScripts, "<[^>]+>", " ");
            var withoutHtmlSpaces = withoutTags.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
            var normalized = Regex.Replace(withoutHtmlSpaces, "\\s+", " ").Trim();

            return !string.IsNullOrWhiteSpace(normalized);
        }
    }
}
