using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Queries.GetCurrent
{
    public class Query : IRequest<Response>
    {
        public int? ClinicId { get; set; }
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
                var clinicId = request.ClinicId;

                if (!clinicId.HasValue)
                {
                    var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                    clinicId = int.TryParse(clinicIdValue, out var currentClinicId) ? currentClinicId : null;
                }

                if (!clinicId.HasValue)
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var item = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.Id == clinicId.Value)
                    .Select(x => new ClinicProfileModel
                    {
                        Id = x.Id,
                        ClinicName = x.ClinicName,
                        Address = x.Address,
                        EmailAddress = x.EmailAddress,
                        ContactNumber = x.ContactNumber,
                        IsDataPrivacyAccepted = x.IsDataPrivacyAccepted,
                        OpeningTime = x.OpeningTime,
                        ClosingTime = x.ClosingTime,
                        LunchStartTime = x.LunchStartTime,
                        LunchEndTime = x.LunchEndTime,
                        WorkingDays = x.WorkingDays
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (item == null)
                {
                    return new BadRequestResponse("Clinic profile was not found.");
                }

                return new SuccessResponse<ClinicProfileModel>(item);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
