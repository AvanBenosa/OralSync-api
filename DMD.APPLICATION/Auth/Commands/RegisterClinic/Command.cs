using DMD.APPLICATION.Auth.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NJsonSchema.Annotations;
using RegisterAuthResponse = DMD.APPLICATION.Auth.Models.AuthResponse;

namespace DMD.APPLICATION.Auth.Commands.RegisterClinic
{
    [JsonSchema("RegisterClinicCommand")]
    public class Command : IRequest<Response>
    {
        public string VerificationCode { get; set; } = string.Empty;
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
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            UserManager<UserProfile> userManager,
            IConfiguration configuration,
            DmdDbContext dbContext,
            IProtectionProvider protectionProvider)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                {
                    return new BadRequestResponse("Password and confirm password do not match.");
                }

                var email = request.Email.Trim();
                if (string.IsNullOrWhiteSpace(email))
                {
                    return new BadRequestResponse("Email address is required.");
                }

                var verificationCode = request.VerificationCode.Trim();
                if (string.IsNullOrWhiteSpace(verificationCode))
                {
                    return new BadRequestResponse("Verification code is required.");
                }

                var verification = await dbContext.ClinicRegistrationVerifications
                    .Where(item => item.EmailAddress == email && item.ConsumedAtUtc == null)
                    .OrderByDescending(item => item.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (verification == null
                    || verification.ExpiresAtUtc < DateTime.UtcNow
                    || !string.Equals(verification.Code, verificationCode, StringComparison.Ordinal))
                {
                    return new BadRequestResponse("Verification code is invalid or expired.");
                }

                var userName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();
                var clinicEmail = request.ClinicEmailAddress.Trim();

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

                var existingClinicEmail = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AnyAsync(item => item.EmailAddress == clinicEmail, cancellationToken);

                if (existingClinicEmail)
                {
                    return new BadRequestResponse("Clinic email address is already in use.");
                }

                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                var clinic = new DOMAIN.Entities.UserProfile.ClinicProfile
                {
                    ClinicName = request.ClinicName.Trim(),
                    Address = request.ClinicAddress.Trim(),
                    EmailAddress = clinicEmail,
                    ContactNumber = request.ClinicContactNumber.Trim(),
                    IsLocked = true
                };

                dbContext.ClinicProfiles.Add(clinic);
                await dbContext.SaveChangesAsync(cancellationToken);

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
                    Role = UserRole.SuperAdmin,
                    IsActive = true,
                    ClinicId = clinic.Id
                };

                var createResult = await userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new BadRequestResponse(string.Join(", ", createResult.Errors.Select(x => x.Description)));
                }

                verification.ConsumedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = AuthResponseFactory.Create(
                    user,
                    configuration,
                    protectionProvider,
                    clinic.ClinicName,
                    clinic.IsDataPrivacyAccepted,
                    clinic.IsContractPolicyAccepted,
                    clinic.ForBetaTestingAccepted,
                    clinic.IsLocked,
                    clinic.BannerImagePath,
                    clinic.Subsciption.ToString(),
                    clinic.ValidityDate.Year > 1 ? clinic.ValidityDate.ToString("O") : string.Empty);
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
