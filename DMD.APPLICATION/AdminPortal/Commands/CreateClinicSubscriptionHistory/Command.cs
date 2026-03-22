using DMD.APPLICATION.AdminPortal.Models;
using DMD.APPLICATION.Auth;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.AdminPortal.Commands.CreateClinicSubscriptionHistory
{
    [JsonSchema("CreateAdminClinicSubscriptionHistoryCommand")]
    public class Command : IRequest<Response>
    {
        public string ClinicId { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
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

                if (!await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.Id == clinicId.Value, cancellationToken))
                {
                    return new BadRequestResponse("Clinic was not found.");
                }

                if (!TryParseSubscriptionType(request.SubscriptionType, out var subscriptionType))
                {
                    return new BadRequestResponse("Invalid subscription type.");
                }

                if (request.TotalAmount < 0)
                {
                    return new BadRequestResponse("Total amount cannot be negative.");
                }

                var newItem = new ClinicSubsciptionHistory
                {
                    ClinicProfileId = clinicId.Value,
                    PaymentDate = request.PaymentDate?.Date ?? DateTime.Today,
                    Subsciption = subscriptionType,
                    TotalAmount = request.TotalAmount
                };

                dbContext.SubsciptionHistories.Add(newItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<AdminClinicSubscriptionHistoryModel>(
                    await AdminClinicSubscriptionHistoryModelFactory.CreateAsync(newItem, protectionProvider));
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }

        private static bool TryParseSubscriptionType(
            string value,
            out SubscriptionType subscriptionType)
        {
            var normalizedValue = value?.Trim() ?? string.Empty;
            if (string.Equals(normalizedValue, "Premium", StringComparison.OrdinalIgnoreCase))
            {
                normalizedValue = nameof(SubscriptionType.Premuim);
            }

            return Enum.TryParse(normalizedValue, true, out subscriptionType);
        }
    }
}
