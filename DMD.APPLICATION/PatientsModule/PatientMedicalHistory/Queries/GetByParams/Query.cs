using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string PatientInfoId { get; set; } = string.Empty;
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly IMapper mapper;
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public QueryHandler(DmdDbContext dbContext, IMapper mapper, IProtectionProvider protectionProvider)
        {
            this.mapper = mapper;
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

                var items = await dbContext.PatientMedicalHistories.AsNoTracking()
                    .Where(x => x.PatientInfoId == patientInfoId)
                    .ToListAsync(cancellationToken);

                var response = await Task.WhenAll(items.Select(async x =>
                {
                    var item = mapper.Map<PatientMedicalHistoryModel>(x);
                    item.Id = await protectionProvider.EncryptIntIdAsync(x.Id, ProtectedIdPurpose.Patient);
                    item.PatientsInfoId = await protectionProvider.EncryptIntIdAsync(x.PatientInfoId, ProtectedIdPurpose.Patient);
                    return item;
                }));

                return new SuccessResponse<List<PatientMedicalHistoryModel>>(response.ToList());
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
