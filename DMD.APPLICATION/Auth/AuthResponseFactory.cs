using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.SERVICES.ProtectionProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DMD.APPLICATION.Auth
{
    internal static class AuthResponseFactory
    {
        internal static string GetSeedAdminEmail(IConfiguration configuration) =>
            configuration["Seed:Admin:Email"]?.Trim() ?? "admin@email.com";

        internal static bool IsBootstrapSeedUser(UserProfile user, IConfiguration configuration)
        {
            var seedEmail = GetSeedAdminEmail(configuration);
            if (string.IsNullOrWhiteSpace(seedEmail))
            {
                return false;
            }

            return string.Equals(user.Email, seedEmail, StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.EmailAddress, seedEmail, StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.UserName, seedEmail, StringComparison.OrdinalIgnoreCase);
        }

        internal static AuthResponse Create(
            UserProfile user,
            IConfiguration configuration,
            IProtectionProvider protectionProvider,
            string? clinicName = null,
            bool isDataPrivacyAccepted = false,
            bool isLocked = false)
        {
            return new AuthResponse
            {
                Token = GenerateJwtToken(user, configuration),
                RequiresRegistration = IsBootstrapSeedUser(user, configuration),
                User = new UserDto
                {
                    Id = protectionProvider.EncryptStringIdAsync(user.Id, ProtectedIdPurpose.User).GetAwaiter().GetResult() ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Name = user.FullName,
                    Email = user.Email ?? user.EmailAddress ?? string.Empty,
                    ClinicId = protectionProvider.EncryptNullableIntIdAsync(user.ClinicId, ProtectedIdPurpose.Clinic).GetAwaiter().GetResult(),
                    ClinicName = clinicName?.Trim() ?? string.Empty,
                    Role = user.Role.ToString().ToLowerInvariant(),
                    RoleLabel = user.RoleLabel,
                    IsDataPrivacyAccepted = isDataPrivacyAccepted,
                    IsLocked = isLocked,
                    ContactNumber = user.ContactNumber,
                    CreatedAt = DateTime.UtcNow.ToString("O"),
                }
            };
        }

        private static string GenerateJwtToken(UserProfile user, IConfiguration configuration)
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
                new("lastName", user.LastName ?? string.Empty),
                new("clinicId", user.ClinicId?.ToString() ?? string.Empty)
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
