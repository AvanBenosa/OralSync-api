using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Queries.GetCurrent
{
    public class Query : IRequest<Response>
    {
        public string? ClinicId { get; set; }
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

                var clinic = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.Id == clinicId.Value)
                    .FirstOrDefaultAsync(cancellationToken);

                if (clinic == null)
                {
                    return new BadRequestResponse("Clinic profile was not found.");
                }

                var item = new ClinicProfileModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(clinic.Id, ProtectedIdPurpose.Clinic),
                        ClinicName = clinic.ClinicName,
                        Address = clinic.Address,
                        EmailAddress = clinic.EmailAddress,
                        ContactNumber = clinic.ContactNumber,
                        IsDataPrivacyAccepted = clinic.IsDataPrivacyAccepted,
                        OpeningTime = clinic.OpeningTime,
                        ClosingTime = clinic.ClosingTime,
                        LunchStartTime = clinic.LunchStartTime,
                        LunchEndTime = clinic.LunchEndTime,
                        WorkingDays = clinic.WorkingDays
                    };

                return new SuccessResponse<ClinicProfileModel>(item);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
