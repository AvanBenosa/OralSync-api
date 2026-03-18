using DMD.APPLICATION.Responses;
using DMD.APPLICATION.UserProfileModule.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.UserProfileModule.Commands.Update
{
    [JsonSchema("UpdateUserProfileCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public Suffix Suffix { get; set; } = Suffix.None;
        public Preffix Preffix { get; set; } = Preffix.None;
        public string Religion { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public EmploymentType EmploymentType { get; set; } = EmploymentType.None;
        public string Bio { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<UserProfile> userManager;
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            UserManager<UserProfile> userManager,
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
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

                if (string.IsNullOrWhiteSpace(request.Id))
                {
                    return new BadRequestResponse("User profile was not found.");
                }

                var decryptedUserId = await protectionProvider.DecryptStringIdAsync(
                    request.Id,
                    ProtectedIdPurpose.User);

                if (string.IsNullOrWhiteSpace(decryptedUserId))
                {
                    return new BadRequestResponse("User profile was not found.");
                }

                var user = await dbContext.UserProfiles
                    .FirstOrDefaultAsync(
                        x => x.Id == decryptedUserId && x.ClinicId == clinicId,
                        cancellationToken);

                if (user == null)
                {
                    return new BadRequestResponse("User profile was not found.");
                }

                var email = request.EmailAddress.Trim();
                var userName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return new BadRequestResponse("Username is required.");
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    return new BadRequestResponse("Email address is required.");
                }

                var existingByEmail = await userManager.FindByEmailAsync(email);
                if (existingByEmail != null && !string.Equals(existingByEmail.Id, user.Id, StringComparison.Ordinal))
                {
                    return new BadRequestResponse("Email address is already in use.");
                }

                var existingByUserName = await userManager.FindByNameAsync(userName);
                if (existingByUserName != null && !string.Equals(existingByUserName.Id, user.Id, StringComparison.Ordinal))
                {
                    return new BadRequestResponse("Username is already in use.");
                }

                user.UserName = userName;
                user.Email = email;
                user.EmailAddress = email;
                user.FirstName = request.FirstName.Trim();
                user.LastName = request.LastName.Trim();
                user.MiddleName = request.MiddleName.Trim();
                user.BirthDate = request.BirthDate;
                user.ContactNumber = request.ContactNumber.Trim();
                user.Address = request.Address.Trim();
                user.Suffix = request.Suffix;
                user.Preffix = request.Preffix;
                user.Religion = request.Religion.Trim();
                user.StartDate = request.StartDate;
                user.EmploymentType = request.EmploymentType;
                user.Bio = request.Bio.Trim();
                user.Role = request.Role;
                user.IsActive = request.IsActive;

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return new BadRequestResponse(string.Join(", ", updateResult.Errors.Select(x => x.Description)));
                }

                var hasPasswordChange = !string.IsNullOrWhiteSpace(request.Password)
                    || !string.IsNullOrWhiteSpace(request.ConfirmPassword);

                if (hasPasswordChange)
                {
                    if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                    {
                        return new BadRequestResponse("Password and confirm password do not match.");
                    }

                    if (string.IsNullOrWhiteSpace(request.Password))
                    {
                        return new BadRequestResponse("Password is required when changing the password.");
                    }

                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await userManager.ResetPasswordAsync(user, resetToken, request.Password);
                    if (!passwordResult.Succeeded)
                    {
                        return new BadRequestResponse(string.Join(", ", passwordResult.Errors.Select(x => x.Description)));
                    }
                }

                return new SuccessResponse<UserProfileModel>(new UserProfileModel
                {
                    Id = await protectionProvider.EncryptStringIdAsync(user.Id, ProtectedIdPurpose.User) ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    EmailAddress = user.EmailAddress,
                    BirthDate = user.BirthDate,
                    ContactNumber = user.ContactNumber,
                    Address = user.Address,
                    Suffix = (int)user.Suffix,
                    Preffix = (int)user.Preffix,
                    Religion = user.Religion,
                    StartDate = user.StartDate,
                    EmploymentType = (int)user.EmploymentType,
                    Bio = user.Bio,
                    Role = (int)user.Role,
                    RoleLabel = user.RoleLabel,
                    ClinicId = await protectionProvider.EncryptNullableIntIdAsync(user.ClinicId, ProtectedIdPurpose.Clinic),
                    IsActive = user.IsActive
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
