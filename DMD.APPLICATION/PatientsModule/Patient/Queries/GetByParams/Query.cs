using AutoMapper;
using AutoMapper.QueryableExtensions;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.Patient.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string ClinicId { get; set; } = string.Empty;
        public string Que { get; set; } = string.Empty;
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
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
                var clinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.ClinicId,
                    ProtectedIdPurpose.Clinic);

                if (!clinicId.HasValue)
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var response = new PatientResponseModel
                {
                    Items = new List<PatientModel>(),
                    PageEnd = request.PageEnd,
                    PageStart = request.PageStart,
                    TotalCount = 0
                };

                var query = dbContext.PatientInfos.AsNoTracking()
                    .Where(x => x.ClinicProfileId == clinicId.Value);

                var keyword = request.Que?.Trim();
                if (!string.IsNullOrWhiteSpace(keyword) && !string.Equals(keyword, "all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedKeyword = keyword.ToLower();
                    query = query.Where(x =>
                        (x.PatientNumber ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.FirstName ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.LastName ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.MiddleName ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.ContactNumber ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.EmailAddress ?? string.Empty).ToLower().Contains(normalizedKeyword));
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var pageStart = Math.Max(request.PageStart, 0);
                var pageSize = request.PageEnd > 0 ? request.PageEnd : 25;

                var items = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip(pageStart)
                    .Take(pageSize)
                    .ProjectTo<PatientModel>(mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                response.Items = (await Task.WhenAll(items.Select(async item =>
                {
                    item.Id = await protectionProvider.EncryptIntIdAsync(int.Parse(item.Id), ProtectedIdPurpose.Patient);
                    item.ClinicProfileId = await protectionProvider.EncryptIntIdAsync(int.Parse(item.ClinicProfileId), ProtectedIdPurpose.Clinic);
                    return item;
                }))).ToList();
                response.TotalCount = totalCount;
                response.PageStart = pageStart;
                response.PageEnd = pageSize;

                return new SuccessResponse<PatientResponseModel>(response);
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
