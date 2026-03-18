using DMD.APPLICATION.Responses;
using DMD.APPLICATION.UserProfileModule.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DMD.APPLICATION.UserProfileModule.Queries.GetByParams
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

                var users = await dbContext.UserProfiles
                    .AsNoTracking()
                    .Where(x => x.ClinicId == clinicId.Value)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync(cancellationToken);

                var items = users.Select(x => new UserProfileModel
                    {
                        Id = protectionProvider.EncryptStringIdAsync(x.Id, ProtectedIdPurpose.User).GetAwaiter().GetResult() ?? string.Empty,
                        UserName = x.UserName ?? string.Empty,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        MiddleName = x.MiddleName,
                        EmailAddress = x.EmailAddress,
                        BirthDate = x.BirthDate,
                        ContactNumber = x.ContactNumber,
                        Address = x.Address,
                        Suffix = (int)x.Suffix,
                        Preffix = (int)x.Preffix,
                        Religion = x.Religion,
                        StartDate = x.StartDate,
                        EmploymentType = (int)x.EmploymentType,
                        Bio = x.Bio,
                        Role = (int)x.Role,
                        RoleLabel = x.RoleLabel,
                        ClinicId = protectionProvider.EncryptNullableIntIdAsync(x.ClinicId, ProtectedIdPurpose.Clinic).GetAwaiter().GetResult(),
                        IsActive = x.IsActive
                    })
                    .ToList();

                return new SuccessResponse<UserProfileResponseModel>(new UserProfileResponseModel
                {
                    Items = items,
                    TotalCount = items.Count
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
