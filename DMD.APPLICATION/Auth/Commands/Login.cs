using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NJsonSchema.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LoginAuthResponse = DMD.APPLICATION.Auth.Models.AuthResponse;

namespace DMD.APPLICATION.Auth.Commands
{
    [JsonSchema("LoginCommand")]
    public class Command : IRequest<Response>
    {
        public string Email { get; set; } = string.Empty;
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
                var user = await userManager.FindByEmailAsync(request.Email);
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

                var token = GenerateJwtToken(user);
                var authResponse = new LoginAuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        Name = user.FullName,
                        Email = user.Email ?? user.EmailAddress ?? string.Empty,
                        Role = user.Role.ToString().ToLowerInvariant(),
                        RoleLabel = user.RoleLabel,
                        ContactNumber = user.ContactNumber
                    }
                };

                return new SuccessResponse<LoginAuthResponse>(authResponse);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }

        private string GenerateJwtToken(UserProfile user)
        {
            var keyValue = configuration["Jwt:Key"] ?? configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new InvalidOperationException("JWT key is missing. Configure Jwt:Key.");
            }

            var issuer = configuration["Jwt:Issuer"] ?? configuration["JwtSettings:Issuer"];
            var audience = configuration["Jwt:Audience"] ?? configuration["JwtSettings:Audience"];
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("JWT issuer/audience is missing.");
            }

            var expiryInMinutesText = configuration["Jwt:ExpiryInMinutes"];
            var expirationInDaysText = configuration["JwtSettings:ExpirationInDays"];
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            if (int.TryParse(expiryInMinutesText, out var expiryInMinutes) && expiryInMinutes > 0)
            {
                expiresAt = DateTime.UtcNow.AddMinutes(expiryInMinutes);
            }
            else if (double.TryParse(expirationInDaysText, out var expirationInDays) && expirationInDays > 0)
            {
                expiresAt = DateTime.UtcNow.AddDays(expirationInDays);
            }

            var key = Encoding.ASCII.GetBytes(keyValue);
            var email = user.Email ?? user.EmailAddress ?? string.Empty;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("firstName", user.FirstName ?? string.Empty),
                new("lastName", user.LastName ?? string.Empty)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
