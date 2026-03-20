using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientForms.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Buildups;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientForms.Commands.Update
{
    [JsonSchema("UpdatePatientFormCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
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
                if (string.IsNullOrWhiteSpace(request.Id))
                {
                    return new BadRequestResponse("Patient form ID is required.");
                }

                if (string.IsNullOrWhiteSpace(request.PatientInfoId))
                {
                    return new BadRequestResponse("PatientInfoId is required.");
                }

                var itemId = await protectionProvider.DecryptIntIdAsync(
                    request.Id,
                    ProtectedIdPurpose.Patient);
                var patientInfoId = await protectionProvider.DecryptIntIdAsync(
                    request.PatientInfoId,
                    ProtectedIdPurpose.Patient);

                var item = await dbContext.PatientForms
                    .FirstOrDefaultAsync(
                        x => x.Id == itemId && x.PatientInfoId == patientInfoId,
                        cancellationToken);

                if (item == null)
                {
                    return new BadRequestResponse("Item may have been modified or removed.");
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

                item.FormTemplateId = templateItem?.Id ?? 0;
                item.AssignedDoctor = request.AssignedDoctor?.Trim() ?? string.Empty;
                item.Remarks = string.IsNullOrWhiteSpace(request.ReportTemplate)
                    ? templateItem?.TemplateContent ?? string.Empty
                    : request.ReportTemplate.Trim();
                item.Date = request.Date ?? item.Date ?? DateTime.Now;

                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<PatientFormModel>(
                    await MapItemAsync(item, templateItem, request.FormType, cancellationToken));
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }

        private async Task<PatientFormModel> MapItemAsync(
            DOMAIN.Entities.Patients.PatientForm item,
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
