using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Queries.GetDataPrivacyStatus
{
    public class Query : IRequest<Response>
    {
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        public QueryHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                if (!int.TryParse(clinicIdValue, out var clinicId))
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var clinic = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.Id == clinicId)
                    .Select(item => new DataPrivacyStatusModel
                    {
                        ClinicId = item.Id,
                        ClinicName = item.ClinicName,
                        IsDataPrivacyAccepted = item.IsDataPrivacyAccepted,
                        IsLocked = item.IsLocked
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (clinic == null)
                {
                    return new NotFoundResponse("Clinic profile was not found.");
                }

                return new SuccessResponse<DataPrivacyStatusModel>(clinic);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
