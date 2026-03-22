using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Finances.DentalInventories.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.Finances.DentalInventories.Queries.GetByParams
{
    [JsonSchema("GetDentalInventoriesByParamsQuery")]
    public class Query : IRequest<Response>
    {
        public string? ClinicId { get; set; }
        public string Que { get; set; } = string.Empty;
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public QueryHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
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
                    var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                    clinicId = int.TryParse(clinicIdValue, out var currentClinicId) ? currentClinicId : null;
                }

                if (!clinicId.HasValue)
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var response = new DentalInventoryResponseModel
                {
                    Items = new List<DentalInventoryModel>(),
                    PageEnd = request.PageEnd,
                    PageStart = request.PageStart,
                    TotalCount = 0
                };

                var query = dbContext.DentalInventories
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.ClinicProfileId == clinicId.Value);

                var keyword = request.Que?.Trim();
                if (!string.IsNullOrWhiteSpace(keyword) &&
                    !string.Equals(keyword, "all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedKeyword = keyword.ToLower();
                    query = query.Where(x =>
                        (x.ItemCode ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.Name ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.Description ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.UnitOfMeasure ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.SupplierName ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.SupplierContactNumber ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.SupplierEmail ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                        (x.BatchNumber ?? string.Empty).ToLower().Contains(normalizedKeyword));
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var pageStart = Math.Max(request.PageStart, 0);
                var pageSize = request.PageEnd > 0 ? request.PageEnd : 25;

                var items = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .Skip(pageStart)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                foreach (var item in items)
                {
                    response.Items.Add(await DentalInventoryModelFactory.CreateAsync(item, protectionProvider));
                }

                response.TotalCount = totalCount;
                response.PageStart = pageStart;
                response.PageEnd = pageSize;

                return new SuccessResponse<DentalInventoryResponseModel>(response);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
