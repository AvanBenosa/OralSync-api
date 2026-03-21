using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Finances.ClinicExpenses.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.Finances.ClinicExpenses.Queries.GetByParams
{
    [JsonSchema("GetClinicExpensesByParamsQuery")]
    public class Query : IRequest<Response>
    {
        public string? ClinicId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
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

                var query = dbContext.ClinicExpenses
                    .AsNoTracking()
                    .Where(x => x.ClinicProfileId == clinicId.Value);

                if (request.DateFrom.HasValue)
                {
                    var dateFrom = request.DateFrom.Value.Date;
                    query = query.Where(x => x.Date >= dateFrom);
                }

                if (request.DateTo.HasValue)
                {
                    var dateToExclusive = request.DateTo.Value.Date.AddDays(1);
                    query = query.Where(x => x.Date < dateToExclusive);
                }

                var items = await query
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync(cancellationToken);

                var responseItems = new List<ClinicExpensesModel>();

                foreach (var item in items)
                {
                    responseItems.Add(new ClinicExpensesModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.ClinicExpense) ?? string.Empty,
                        ClinicProfileId = await protectionProvider.EncryptIntIdAsync(item.ClinicProfileId, ProtectedIdPurpose.Clinic) ?? string.Empty,
                        Remarks = item.Remarks,
                        Category = item.Category,
                        Date = item.Date,
                        Amount = item.Amount
                    });
                }

                return new SuccessResponse<List<ClinicExpensesModel>>(responseItems);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
