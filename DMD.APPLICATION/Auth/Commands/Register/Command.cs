using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NJsonSchema.Annotations;
using System.Security.Claims;
using RegisterAuthResponse = DMD.APPLICATION.Auth.Models.AuthResponse;

namespace DMD.APPLICATION.Auth.Commands.Register
{
    [JsonSchema("RegisterBootstrapCommand")]
    public class Command : IRequest<Response>
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public Suffix Suffix { get; set; } = Suffix.None;
        public Preffix Preffix { get; set; } = Preffix.None;
        public string Religion { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public EmploymentType EmploymentType { get; set; } = EmploymentType.None;
        public string Bio { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.SuperAdmin;
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicAddress { get; set; } = string.Empty;
        public string ClinicEmailAddress { get; set; } = string.Empty;
        public string ClinicContactNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly UserManager<UserProfile> userManager;
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            UserManager<UserProfile> userManager,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            DmdDbContext dbContext,
            IProtectionProvider protectionProvider)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new BadRequestResponse("Authenticated user was not found.");
                }

                var currentUser = await userManager.FindByIdAsync(currentUserId);
                if (currentUser == null)
                {
                    return new BadRequestResponse("Authenticated user was not found.");
                }

                if (!AuthResponseFactory.IsBootstrapSeedUser(currentUser, configuration))
                {
                    return new BadRequestResponse("Registration bootstrap is only available for the seeded admin account.");
                }

                if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                {
                    return new BadRequestResponse("Password and confirm password do not match.");
                }

                if (string.IsNullOrWhiteSpace(request.ClinicName))
                {
                    return new BadRequestResponse("Clinic name is required.");
                }

                if (string.IsNullOrWhiteSpace(request.ClinicAddress))
                {
                    return new BadRequestResponse("Clinic address is required.");
                }

                if (string.IsNullOrWhiteSpace(request.ClinicEmailAddress))
                {
                    return new BadRequestResponse("Clinic email address is required.");
                }

                if (string.IsNullOrWhiteSpace(request.ClinicContactNumber))
                {
                    return new BadRequestResponse("Clinic contact number is required.");
                }

                var email = request.Email.Trim();
                var userName = string.IsNullOrWhiteSpace(request.UserName)
                    ? email
                    : request.UserName.Trim();

                var clinicEmail = request.ClinicEmailAddress.Trim();

                var existingByEmail = await userManager.FindByEmailAsync(email);
                if (existingByEmail != null && existingByEmail.Id != currentUser.Id)
                {
                    return new BadRequestResponse("Email address is already in use.");
                }

                var existingByUserName = await userManager.FindByNameAsync(userName);
                if (existingByUserName != null && existingByUserName.Id != currentUser.Id)
                {
                    return new BadRequestResponse("Username is already in use.");
                }

                var newUser = new UserProfile
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
                    IsActive = true
                };

                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                var clinic = new ClinicProfile
                {
                    ClinicName = request.ClinicName.Trim(),
                    Address = request.ClinicAddress.Trim(),
                    EmailAddress = clinicEmail,
                    ContactNumber = request.ClinicContactNumber.Trim()
                };

                dbContext.ClinicProfiles.Add(clinic);
                await dbContext.SaveChangesAsync(cancellationToken);

                newUser.ClinicId = clinic.Id;

                var createResult = await userManager.CreateAsync(newUser, request.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new BadRequestResponse(string.Join(", ", createResult.Errors.Select(x => x.Description)));
                }

                var deleteSeedResult = await userManager.DeleteAsync(currentUser);
                if (!deleteSeedResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new BadRequestResponse(string.Join(", ", deleteSeedResult.Errors.Select(x => x.Description)));
                }

                await transaction.CommitAsync(cancellationToken);

                var response = AuthResponseFactory.Create(
                    newUser,
                    configuration,
                    protectionProvider,
                    clinic.ClinicName,
                    clinic.IsDataPrivacyAccepted);
                response.RequiresRegistration = false;

                return new SuccessResponse<RegisterAuthResponse>(response);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
