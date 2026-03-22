using DMD.APPLICATION.AdminPortal.Models;
using DMD.APPLICATION.Auth;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.AdminPortal.Queries.GetClinics
{
    [JsonSchema("GetAdminClinicsQuery")]
    public class Query : IRequest<Response>
    {
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;

        public QueryHandler(
            DmdDbContext dbContext,
            IProtectionProvider protectionProvider,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new BadRequestResponse("Unauthorized access.");
                }

                var currentUser = await dbContext.UserProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

                if (currentUser == null || !AuthResponseFactory.IsBootstrapSeedUser(currentUser, configuration))
                {
                    return new BadRequestResponse("Admin portal access is restricted.");
                }

                var clinics = await dbContext.ClinicProfiles
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .OrderBy(x => x.ClinicName)
                    .Select(x => new
                    {
                        x.Id,
                        x.ClinicName,
                        x.Address,
                        x.EmailAddress,
                        x.ContactNumber,
                        x.ValidityDate,
                        x.Subsciption,
                        x.IsLocked,
                        x.IsDataPrivacyAccepted,
                        x.CreatedAt,
                        OwnerName = dbContext.UserProfiles
                            .Where(user => user.ClinicId == x.Id)
                            .OrderByDescending(user => user.Role)
                            .Select(user => user.FullName)
                            .FirstOrDefault(),
                        TotalUsers = dbContext.UserProfiles.Count(user => user.ClinicId == x.Id)
                    })
                    .ToListAsync(cancellationToken);

                var response = await Task.WhenAll(clinics.Select(async x => new AdminClinicModel
                {
                    Id = await protectionProvider.EncryptIntIdAsync(x.Id, ProtectedIdPurpose.Clinic) ?? string.Empty,
                    ClinicName = x.ClinicName,
                    Address = x.Address,
                    EmailAddress = x.EmailAddress,
                    ContactNumber = x.ContactNumber,
                    SubscriptionType = x.Subsciption.ToString(),
                    ValidityDate = x.ValidityDate.ToString("O"),
                    IsLocked = x.IsLocked,
                    IsDataPrivacyAccepted = x.IsDataPrivacyAccepted,
                    CreatedAt = x.CreatedAt.ToString("O"),
                    OwnerName = x.OwnerName ?? string.Empty,
                    TotalUsers = x.TotalUsers
                }));

                return new SuccessResponse<List<AdminClinicModel>>(response.ToList());
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
