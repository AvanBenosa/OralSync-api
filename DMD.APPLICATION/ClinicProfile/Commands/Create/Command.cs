using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Commands.Create
{
    [JsonSchema("CreateClinicProfileCommand")]
    public class Command : IRequest<Response>
    {
        public string ClinicName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<UserProfile> userManager;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            UserManager<UserProfile> userManager,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ClinicName))
                {
                    return new BadRequestResponse("Clinic name is required.");
                }

                var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return new BadRequestResponse("Authenticated user was not found.");
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new BadRequestResponse("Authenticated user was not found.");
                }

                if (user.ClinicId.HasValue)
                {
                    return new BadRequestResponse("Clinic profile already exists for this user.");
                }

                var item = new ClinicProfile
                {
                    ClinicName = request.ClinicName.Trim(),
                    Address = request.Address.Trim(),
                    EmailAddress = request.EmailAddress.Trim(),
                    ContactNumber = request.ContactNumber.Trim()
                };

                dbContext.ClinicProfiles.Add(item);
                await dbContext.SaveChangesAsync(cancellationToken);

                user.ClinicId = item.Id;
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return new BadRequestResponse(string.Join(", ", updateResult.Errors.Select(x => x.Description)));
                }

                return new SuccessResponse<ClinicProfileModel>(new ClinicProfileModel
                {
                    Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Clinic),
                    ClinicName = item.ClinicName,
                    Address = item.Address,
                    EmailAddress = item.EmailAddress,
                    ContactNumber = item.ContactNumber,
                    IsDataPrivacyAccepted = item.IsDataPrivacyAccepted,
                    SubscriptionType = item.Subsciption.ToString(),
                    ValidityDate = item.ValidityDate.Year > 1 ? item.ValidityDate.ToString("O") : string.Empty
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
