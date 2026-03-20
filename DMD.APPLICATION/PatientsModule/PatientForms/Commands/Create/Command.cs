using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientForms.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Buildups;
using DMD.DOMAIN.Entities.Patients;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientForms.Commands.Create
{
    [JsonSchema("CreatePatientFormCommand")]
    public class Command : IRequest<Response>
    {
        public string PatientInfoId { get; set; } = string.Empty;
        public string TemplateFormId { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public string ReportTemplate { get; set; } = string.Empty;
        public string AssignedDoctor { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
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
                if (string.IsNullOrWhiteSpace(request.PatientInfoId))
                {
                    return new BadRequestResponse("PatientInfoId is required.");
                }

                var patientInfoId = await protectionProvider.DecryptIntIdAsync(
                    request.PatientInfoId,
                    ProtectedIdPurpose.Patient);

                var patientExists = await dbContext.PatientInfos
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == patientInfoId, cancellationToken);

                if (!patientExists)
                {
                    return new BadRequestResponse("Patient was not found.");
                }

                FormTemplate? templateItem = null;
                if (!string.IsNullOrWhiteSpace(request.TemplateFormId))
                {
                    var templateFormId = await protectionProvider.DecryptIntIdAsync(
                        request.TemplateFormId,
                        ProtectedIdPurpose.FormTemplate);

                    templateItem = await dbContext.FormTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == templateFormId, cancellationToken);

                    if (templateItem == null)
                    {
                        return new BadRequestResponse("Template form was not found.");
                    }
                }

                var reportTemplate = string.IsNullOrWhiteSpace(request.ReportTemplate)
                    ? templateItem?.TemplateContent ?? string.Empty
                    : request.ReportTemplate.Trim();

                var newItem = new PatientForm
                {
                    PatientInfoId = patientInfoId,
                    FormTemplateId = templateItem?.Id ?? 0,
                    AssignedDoctor = request.AssignedDoctor?.Trim() ?? string.Empty,
                    Remarks = reportTemplate,
                    Date = request.Date ?? DateTime.Now,
                };

                dbContext.PatientForms.Add(newItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<PatientFormModel>(
                    await MapItemAsync(newItem, templateItem, request.FormType, cancellationToken));
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }

        private async Task<PatientFormModel> MapItemAsync(
            PatientForm item,
            FormTemplate? templateItem,
            string? fallbackFormType,
            CancellationToken cancellationToken)
        {
            if (templateItem == null && item.FormTemplateId > 0)
            {
                templateItem = await dbContext.FormTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == item.FormTemplateId, cancellationToken);
            }

            return new PatientFormModel
            {
                Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient),
                PatientInfoId = await protectionProvider.EncryptIntIdAsync(
                    item.PatientInfoId,
                    ProtectedIdPurpose.Patient),
                TemplateFormId = item.FormTemplateId > 0
                    ? await protectionProvider.EncryptIntIdAsync(
                        item.FormTemplateId,
                        ProtectedIdPurpose.FormTemplate) ?? string.Empty
                    : string.Empty,
                FormType = templateItem?.TemplateName
                    ?? fallbackFormType?.Trim()
                    ?? (item.FormTemplateId > 0 ? string.Empty : "Custom Form"),
                ReportTemplate = string.IsNullOrWhiteSpace(item.Remarks)
                    ? templateItem?.TemplateContent ?? string.Empty
                    : item.Remarks,
                AssignedDoctor = item.AssignedDoctor ?? string.Empty,
                Date = item.Date,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.LastUpdatedAt
            };
        }
    }
}
