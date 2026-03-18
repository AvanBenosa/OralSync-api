using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NJsonSchema.Annotations;
using LoginAuthResponse = DMD.APPLICATION.Auth.Models.AuthResponse;

namespace DMD.APPLICATION.Auth.Commands
{
    [JsonSchema("LoginCommand")]
    public class Command : IRequest<Response>
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<UserProfile> userManager;
        private readonly SignInManager<UserProfile> signInManager;
        private readonly IConfiguration configuration;
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            UserManager<UserProfile> userManager,
            SignInManager<UserProfile> signInManager,
            IConfiguration configuration,
            DmdDbContext dbContext,
            IProtectionProvider protectionProvider)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var identifier = request.Email?.Trim();
                var alternateIdentifier = request.Username?.Trim();

                UserProfile? user = null;
                if (!string.IsNullOrWhiteSpace(identifier))
                {
                    user = await userManager.FindByEmailAsync(identifier);
                    user ??= await userManager.FindByNameAsync(identifier);
                }

                if (user == null && !string.IsNullOrWhiteSpace(alternateIdentifier))
                {
                    user = await userManager.FindByEmailAsync(alternateIdentifier);
                    user ??= await userManager.FindByNameAsync(alternateIdentifier);
                }

                if (user == null)
                {
                    return new BadRequestResponse("Invalid email or password");
                }

                var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return new BadRequestResponse("Invalid email or password");
                }

                if (!user.IsActive)
                {
                    return new BadRequestResponse("Account is inactive");
                }

                var clinicName = string.Empty;
                var isDataPrivacyAccepted = false;
                var isLocked = false;
                if (user.ClinicId.HasValue)
                {
                    var facility = await dbContext.ClinicProfiles.AsNoTracking()
                        .Where(x => x.Id == user.ClinicId)
                        .Select(x => new { x.ClinicName, x.IsDataPrivacyAccepted, x.IsLocked })
                        .FirstOrDefaultAsync();

                    if (facility != null)
                    {
                        clinicName = facility.ClinicName;
                        isDataPrivacyAccepted = facility.IsDataPrivacyAccepted;
                        isLocked = facility.IsLocked;
                    }
                }

                var authResponse = AuthResponseFactory.Create(
                    user,
                    configuration,
                    protectionProvider,
                    clinicName,
                    isDataPrivacyAccepted,
                    isLocked);

                return new SuccessResponse<LoginAuthResponse>(authResponse);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
