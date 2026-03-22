using DMD.APPLICATION.AdminPortal.Models;
using DMD.APPLICATION.Auth;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NJsonSchema.Annotations;
using System.Globalization;
using System.Security.Claims;

namespace DMD.APPLICATION.AdminPortal.Commands.UpdateClinicLock
{
    [JsonSchema("UpdateAdminClinicLockCommand")]
    public class Command : IRequest<Response>
    {
        public string ClinicId { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public string ValidityDate { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CommandHandler(
            DmdDbContext dbContext,
            IProtectionProvider protectionProvider,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new BadRequestResponse("Unauthorized access.");
                }

                var currentUser = await dbContext.UserProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

                if (currentUser == null || !AuthResponseFactory.IsBootstrapSeedUser(currentUser, configuration))
                {
                    return new BadRequestResponse("Admin portal access is restricted.");
                }

                var clinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.ClinicId,
                    ProtectedIdPurpose.Clinic);

                if (!clinicId.HasValue)
                {
                    return new BadRequestResponse("Clinic was not found.");
                }

                var clinic = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == clinicId.Value, cancellationToken);

                if (clinic == null)
                {
                    return new BadRequestResponse("Clinic was not found.");
                }

                if (!string.IsNullOrWhiteSpace(request.SubscriptionType))
                {
                    var normalizedSubscriptionType = request.SubscriptionType.Trim();
                    if (string.Equals(normalizedSubscriptionType, "Premium", StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedSubscriptionType = nameof(SubscriptionType.Premuim);
                    }

                    if (!Enum.TryParse(normalizedSubscriptionType, true, out SubscriptionType parsedSubscriptionType))
                    {
                        return new BadRequestResponse("Invalid subscription type.");
                    }

                    clinic.Subsciption = parsedSubscriptionType;
                }

                if (!string.IsNullOrWhiteSpace(request.ValidityDate))
                {
                    if (!DateTime.TryParse(
                        request.ValidityDate,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal,
                        out var parsedValidityDate))
                    {
                        return new BadRequestResponse("Invalid validity date.");
                    }

                    clinic.ValidityDate = parsedValidityDate.Date;
                }

                clinic.IsLocked = request.IsLocked;
                await dbContext.SaveChangesAsync(cancellationToken);

                var ownerName = await dbContext.UserProfiles
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(user => user.ClinicId == clinic.Id)
                    .OrderByDescending(user => user.Role)
                    .Select(user => user.FullName)
                    .FirstOrDefaultAsync(cancellationToken);

                var totalUsers = await dbContext.UserProfiles
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .CountAsync(user => user.ClinicId == clinic.Id, cancellationToken);

                return new SuccessResponse<AdminClinicModel>(new AdminClinicModel
                {
                    Id = await protectionProvider.EncryptIntIdAsync(clinic.Id, ProtectedIdPurpose.Clinic) ?? string.Empty,
                    ClinicName = clinic.ClinicName,
                    Address = clinic.Address,
                    EmailAddress = clinic.EmailAddress,
                    ContactNumber = clinic.ContactNumber,
                    SubscriptionType = clinic.Subsciption.ToString(),
                    ValidityDate = clinic.ValidityDate.ToString("O"),
                    IsLocked = clinic.IsLocked,
                    IsDataPrivacyAccepted = clinic.IsDataPrivacyAccepted,
                    CreatedAt = clinic.CreatedAt.ToString("O"),
                    OwnerName = ownerName ?? string.Empty,
                    TotalUsers = totalUsers
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
