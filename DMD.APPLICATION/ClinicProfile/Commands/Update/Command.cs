using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Commands.Update
{
    [JsonSchema("UpdateClinicProfileCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string BannerImagePath { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public string OpeningTime { get; set; } = "09:00";
        public string ClosingTime { get; set; } = "18:00";
        public string LunchStartTime { get; set; } = "12:00";
        public string LunchEndTime { get; set; } = "13:00";
        public bool IsMondayOpen { get; set; } = true;
        public bool IsTuesdayOpen { get; set; } = true;
        public bool IsWednesdayOpen { get; set; } = true;
        public bool IsThursdayOpen { get; set; } = true;
        public bool IsFridayOpen { get; set; } = true;
        public bool IsSaturdayOpen { get; set; }
        public bool IsSundayOpen { get; set; }
        public List<string> WorkingDays { get; set; } = new() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                var requestClinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.Id,
                    ProtectedIdPurpose.Clinic);
                var currentClinicId = int.TryParse(clinicIdValue, out var clinicId)
                    ? clinicId
                    : requestClinicId;

                if (!currentClinicId.HasValue)
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var item = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == currentClinicId.Value, cancellationToken);

                if (item == null)
                {
                    return new BadRequestResponse("Clinic profile was not found.");
                }

                item.ClinicName = request.ClinicName.Trim();
                item.BannerImagePath = request.BannerImagePath.Trim();
                item.Address = request.Address.Trim();
                item.EmailAddress = request.EmailAddress.Trim();
                item.ContactNumber = request.ContactNumber.Trim();
                item.IsDataPrivacyAccepted = request.IsDataPrivacyAccepted;
                item.OpeningTime = request.OpeningTime.Trim();
                item.ClosingTime = request.ClosingTime.Trim();
                item.LunchStartTime = request.LunchStartTime.Trim();
                item.LunchEndTime = request.LunchEndTime.Trim();
                item.IsMondayOpen = request.IsMondayOpen;
                item.IsTuesdayOpen = request.IsTuesdayOpen;
                item.IsWednesdayOpen = request.IsWednesdayOpen;
                item.IsThursdayOpen = request.IsThursdayOpen;
                item.IsFridayOpen = request.IsFridayOpen;
                item.IsSaturdayOpen = request.IsSaturdayOpen;
                item.IsSundayOpen = request.IsSundayOpen;

                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<ClinicProfileModel>(new ClinicProfileModel
                {
                    Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Clinic),
                    ClinicName = item.ClinicName,
                    BannerImagePath = item.BannerImagePath,
                    QrCodeValue = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Clinic),
                    Address = item.Address,
                    EmailAddress = item.EmailAddress,
                    ContactNumber = item.ContactNumber,
                    IsDataPrivacyAccepted = item.IsDataPrivacyAccepted,
                    OpeningTime = item.OpeningTime,
                    ClosingTime = item.ClosingTime,
                    LunchStartTime = item.LunchStartTime,
                    LunchEndTime = item.LunchEndTime,
                    IsMondayOpen = item.IsMondayOpen,
                    IsTuesdayOpen = item.IsTuesdayOpen,
                    IsWednesdayOpen = item.IsWednesdayOpen,
                    IsThursdayOpen = item.IsThursdayOpen,
                    IsFridayOpen = item.IsFridayOpen,
                    IsSaturdayOpen = item.IsSaturdayOpen,
                    IsSundayOpen = item.IsSundayOpen,
                    SubscriptionType = item.Subsciption.ToString(),
                    ValidityDate = item.ValidityDate.Year > 1 ? item.ValidityDate.ToString("O") : string.Empty,
                });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
