using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using MediatR;
using Microsoft.AspNetCore.Identity;
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

        public CommandHandler(
            UserManager<UserProfile> userManager,
            SignInManager<UserProfile> signInManager,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
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

                var authResponse = AuthResponseFactory.Create(user, configuration);

                return new SuccessResponse<LoginAuthResponse>(authResponse);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
