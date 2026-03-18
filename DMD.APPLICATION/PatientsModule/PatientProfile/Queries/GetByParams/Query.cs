using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientProfile.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientProfile.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string PatientId { get; set; } = string.Empty;
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
                var patientId = await protectionProvider.DecryptIntIdAsync(request.PatientId, ProtectedIdPurpose.Patient);
                var item = await dbContext.PatientInfos
                    .AsNoTracking()
                    .Where(x => x.Id == patientId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (item == null)
                {
                    return new BadRequestResponse("Patient not found.");
                }

                var result = mapper.Map<PatientProfileModel>(item);
                result.Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient);
                return new SuccessResponse<PatientProfileModel>(result);
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
