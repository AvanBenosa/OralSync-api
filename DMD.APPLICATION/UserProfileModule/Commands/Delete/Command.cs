using DMD.APPLICATION.Responses;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.UserProfileModule.Commands.Delete
{
    [JsonSchema("DeleteUserProfileCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<UserProfile> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            UserManager<UserProfile> userManager,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                if (!int.TryParse(clinicIdValue, out var clinicId))
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var decryptedUserId = await protectionProvider.DecryptStringIdAsync(
                    request.Id,
                    ProtectedIdPurpose.User);

                if (string.IsNullOrWhiteSpace(decryptedUserId))
                {
                    return new BadRequestResponse("User profile was not found.");
                }

                var user = await userManager.Users
                    .FirstOrDefaultAsync(
                        x => x.Id == decryptedUserId && x.ClinicId == clinicId,
                        cancellationToken);

                if (user == null)
                {
                    return new BadRequestResponse("User profile was not found.");
                }

                if (user.Role == UserRole.SuperAdmin)
                {
                    return new BadRequestResponse("Super admin accounts cannot be deleted.");
                }

                var deleteResult = await userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    return new BadRequestResponse(string.Join(", ", deleteResult.Errors.Select(x => x.Description)));
                }

                return new SuccessResponse<string>(await protectionProvider.EncryptStringIdAsync(user.Id, ProtectedIdPurpose.User) ?? string.Empty);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
