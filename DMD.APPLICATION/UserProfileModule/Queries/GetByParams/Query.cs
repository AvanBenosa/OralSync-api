using DMD.APPLICATION.Responses;
using DMD.APPLICATION.UserProfileModule.Models;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DMD.APPLICATION.UserProfileModule.Queries.GetByParams
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

                var items = await dbContext.UserProfiles
                    .AsNoTracking()
                    .Where(x => x.ClinicId == clinicId.Value)
                    .OrderByDescending(x => x.Id)
                    .Select(x => new UserProfileModel
                    {
                        Id = x.Id,
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
                        ClinicId = x.ClinicId,
                        IsActive = x.IsActive
                    })
                    .ToListAsync(cancellationToken);

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
