using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientForms.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Buildups;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientForms.Queries.GetByParams
{
    [JsonSchema("GetPatientFormByParamsQuery")]
    public class Query : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public QueryHandler(DmdDbContext dbContext, IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var patientInfoId = await protectionProvider.DecryptIntIdAsync(
                    request.PatientInfoId,
                    ProtectedIdPurpose.Patient);

                var itemsQuery = dbContext.PatientForms
                    .AsNoTracking()
                    .Where(x => x.PatientInfoId == patientInfoId);

                if (!string.IsNullOrWhiteSpace(request.Id))
                {
                    var itemId = await protectionProvider.DecryptIntIdAsync(
                        request.Id,
                        ProtectedIdPurpose.Patient);
                    itemsQuery = itemsQuery.Where(x => x.Id == itemId);
                }

                var items = await itemsQuery
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.CreatedAt)
                    .ToListAsync(cancellationToken);

                var templateIds = items
                    .Where(x => x.FormTemplateId > 0)
                    .Select(x => x.FormTemplateId)
                    .Distinct()
                    .ToList();

                var templateLookup = await dbContext.FormTemplates
                    .AsNoTracking()
                    .Where(x => templateIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

                var responseItems = new List<PatientFormModel>();

                foreach (var item in items)
                {
                    templateLookup.TryGetValue(item.FormTemplateId, out FormTemplate? templateItem);

                    responseItems.Add(new PatientFormModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(
                            item.Id,
                            ProtectedIdPurpose.Patient),
                        PatientInfoId = await protectionProvider.EncryptIntIdAsync(
                            item.PatientInfoId,
                            ProtectedIdPurpose.Patient),
                        TemplateFormId = item.FormTemplateId > 0
                            ? await protectionProvider.EncryptIntIdAsync(
                                item.FormTemplateId,
                                ProtectedIdPurpose.FormTemplate) ?? string.Empty
                            : string.Empty,
                        FormType = templateItem?.TemplateName
                            ?? (item.FormTemplateId > 0 ? string.Empty : "Custom Form"),
                        ReportTemplate = string.IsNullOrWhiteSpace(item.Remarks)
                            ? templateItem?.TemplateContent ?? string.Empty
                            : item.Remarks,
                        AssignedDoctor = item.AssignedDoctor ?? string.Empty,
                        Date = item.Date,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.LastUpdatedAt
                    });
                }

                return new SuccessResponse<List<PatientFormModel>>(responseItems);
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
    }
}
