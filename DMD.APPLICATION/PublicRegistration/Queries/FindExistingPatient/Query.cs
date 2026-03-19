using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PublicRegistration.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PublicRegistration.Queries.FindExistingPatient
{
    [JsonSchema("FindPublicExistingPatientQuery")]
    public class Query : IRequest<Response>
    {
        public string ClinicId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public QueryHandler(DmdDbContext dbContext, IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ClinicId))
                    return new BadRequestResponse("Clinic id is required.");

                if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                    return new BadRequestResponse("Patient first name and last name are required.");

                if (string.IsNullOrWhiteSpace(request.EmailAddress))
                    return new BadRequestResponse("Email address is required.");

                var clinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.ClinicId,
                    ProtectedIdPurpose.Clinic);

                if (!clinicId.HasValue)
                    return new BadRequestResponse("Clinic was not found.");

                var firstName = request.FirstName.Trim().ToLower();
                var lastName = request.LastName.Trim().ToLower();
                var emailAddress = request.EmailAddress.Trim().ToLower();

                var patient = await dbContext.PatientInfos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.ClinicProfileId == clinicId.Value &&
                            x.FirstName.ToLower() == firstName &&
                            x.LastName.ToLower() == lastName &&
                            x.EmailAddress.ToLower() == emailAddress,
                        cancellationToken);

                if (patient == null)
                    return new BadRequestResponse("No patient record matched the provided information.");

                return new SuccessResponse<PublicExistingPatientLookupModel>(
                    new PublicExistingPatientLookupModel
                    {
                        PatientId = await protectionProvider.EncryptIntIdAsync(
                            patient.Id,
                            ProtectedIdPurpose.Patient),
                        PatientNumber = patient.PatientNumber ?? string.Empty,
                        FirstName = patient.FirstName ?? string.Empty,
                        LastName = patient.LastName ?? string.Empty,
                        EmailAddress = patient.EmailAddress ?? string.Empty,
                    });
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
            finally
            {
                await dbContext.DisposeAsync();
            }
        }
    }
}
