using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.Auth.Queries.GetRegistrationStatus
{
    [JsonSchema("GetRegistrationStatusQuery")]
    public class Query : IRequest<Response>
    {
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly UserManager<UserProfile> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;

        public QueryHandler(
            UserManager<UserProfile> userManager,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new SuccessResponse<RegistrationStatusResponse>(new RegistrationStatusResponse
                    {
                        RequiresRegistration = false
                    });
                }

                var currentUser = await userManager.FindByIdAsync(currentUserId);
                var requiresRegistration = currentUser != null
                    && AuthResponseFactory.IsBootstrapSeedUser(currentUser, configuration);

                return new SuccessResponse<RegistrationStatusResponse>(new RegistrationStatusResponse
                {
                    RequiresRegistration = requiresRegistration
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
