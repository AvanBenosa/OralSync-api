using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DMD.APPLICATION.ClinicProfiles.Queries.GetCurrent
{
    public class Query : IRequest<Response>
    {
        public string? ClinicId { get; set; }
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public QueryHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var clinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.ClinicId,
                    ProtectedIdPurpose.Clinic);

                if (!clinicId.HasValue)
                {
                    var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                    clinicId = int.TryParse(clinicIdValue, out var currentClinicId) ? currentClinicId : null;
                }

                if (!clinicId.HasValue)
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var clinic = await dbContext.ClinicProfiles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.Id == clinicId.Value)
                    .FirstOrDefaultAsync(cancellationToken);

                if (clinic == null)
                {
                    return new BadRequestResponse("Clinic profile was not found.");
                }

                var patientCount = await dbContext.PatientInfos
                    .AsNoTracking()
                    .CountAsync(x => x.ClinicProfileId == clinicId.Value, cancellationToken);

                var userCount = await dbContext.UserProfiles
                    .AsNoTracking()
                    .CountAsync(x => x.ClinicId == clinicId.Value, cancellationToken);

                var profilePictureCount = await dbContext.PatientInfos
                    .AsNoTracking()
                    .CountAsync(
                        x => x.ClinicProfileId == clinicId.Value && !string.IsNullOrEmpty(x.ProfilePicture),
                        cancellationToken);

                var patientUploadCount = await (
                    from upload in dbContext.PatientUploads.AsNoTracking()
                    join patient in dbContext.PatientInfos.AsNoTracking()
                        on upload.PatientInfoId equals patient.Id
                    where patient.ClinicProfileId == clinicId.Value
                    select upload.Id
                ).CountAsync(cancellationToken);

                var dentalPhotoCount = await (
                    from image in dbContext.PatientTeethImages.AsNoTracking()
                    join teeth in dbContext.PatientTeeth.AsNoTracking()
                        on image.PatientTeethId equals teeth.Id
                    join patient in dbContext.PatientInfos.AsNoTracking()
                        on teeth.PatientInfoId equals patient.Id
                    where patient.ClinicProfileId == clinicId.Value
                    select image.Id
                ).CountAsync(cancellationToken);

                var item = new ClinicProfileModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(clinic.Id, ProtectedIdPurpose.Clinic),
                        ClinicName = clinic.ClinicName,
                        BannerImagePath = clinic.BannerImagePath,
                        QrCodeValue = await protectionProvider.EncryptIntIdAsync(clinic.Id, ProtectedIdPurpose.Clinic),
                        Address = clinic.Address,
                        EmailAddress = clinic.EmailAddress,
                        ContactNumber = clinic.ContactNumber,
                        IsDataPrivacyAccepted = clinic.IsDataPrivacyAccepted,
                        IsContractPolicyAccepted = clinic.IsContractPolicyAccepted,
                        ForBetaTestingAccepted = clinic.ForBetaTestingAccepted,
                        OpeningTime = clinic.OpeningTime,
                        ClosingTime = clinic.ClosingTime,
                        LunchStartTime = clinic.LunchStartTime,
                        LunchEndTime = clinic.LunchEndTime,
                        IsMondayOpen = clinic.IsMondayOpen,
                        IsTuesdayOpen = clinic.IsTuesdayOpen,
                        IsWednesdayOpen = clinic.IsWednesdayOpen,
                        IsThursdayOpen = clinic.IsThursdayOpen,
                        IsFridayOpen = clinic.IsFridayOpen,
                        IsSaturdayOpen = clinic.IsSaturdayOpen,
                        IsSundayOpen = clinic.IsSundayOpen,
                        SubscriptionType = clinic.Subsciption.ToString(),
                        ValidityDate = clinic.ValidityDate.Year > 1 ? clinic.ValidityDate.ToString("O") : string.Empty,
                        PatientCount = patientCount,
                        UploadedFileCount = profilePictureCount + patientUploadCount + dentalPhotoCount,
                        UserCount = userCount,
                    };

                return new SuccessResponse<ClinicProfileModel>(item);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
