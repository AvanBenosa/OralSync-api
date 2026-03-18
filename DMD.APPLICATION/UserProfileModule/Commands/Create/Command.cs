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

namespace DMD.APPLICATION.UserProfileModule.Commands.Create
{
    [JsonSchema("CreateUserProfileCommand")]
    public class Command : IRequest<Response>
    {
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

                if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                {
                    return new BadRequestResponse("Password and confirm password do not match.");
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
                if (existingByEmail != null)
                {
                    return new BadRequestResponse("Email address is already in use.");
                }

                var existingByUserName = await userManager.FindByNameAsync(userName);
                if (existingByUserName != null)
                {
                    return new BadRequestResponse("Username is already in use.");
                }

                var clinicExists = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.Id == clinicId, cancellationToken);

                if (!clinicExists)
                {
                    return new BadRequestResponse("Clinic profile was not found.");
                }

                var user = new UserProfile
                {
                    UserName = userName,
                    Email = email,
                    EmailAddress = email,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    MiddleName = request.MiddleName.Trim(),
                    BirthDate = request.BirthDate,
                    ContactNumber = request.ContactNumber.Trim(),
                    Address = request.Address.Trim(),
                    Suffix = request.Suffix,
                    Preffix = request.Preffix,
                    Religion = request.Religion.Trim(),
                    StartDate = request.StartDate,
                    EmploymentType = request.EmploymentType,
                    Bio = request.Bio.Trim(),
                    Role = request.Role,
                    IsActive = request.IsActive,
                    ClinicId = clinicId
                };

                var createResult = await userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    return new BadRequestResponse(string.Join(", ", createResult.Errors.Select(x => x.Description)));
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
