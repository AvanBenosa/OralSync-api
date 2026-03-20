using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientDentalPhotos.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientDentalPhotos.Queries.GetByParams
{
    [JsonSchema("GetPatientDentalPhotosByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string PatientInfoId { get; set; } = string.Empty;
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
                if (string.IsNullOrWhiteSpace(request.PatientInfoId))
                {
                    return new BadRequestResponse("PatientInfoId is required.");
                }

                var patientInfoId = await protectionProvider.DecryptIntIdAsync(
                    request.PatientInfoId,
                    ProtectedIdPurpose.Patient);

                var items = await (
                    from image in dbContext.PatientTeethImages.AsNoTracking()
                    join teeth in dbContext.PatientTeeth.AsNoTracking()
                        on image.PatientTeethId equals teeth.Id
                    where teeth.PatientInfoId == patientInfoId
                    orderby teeth.ToothNumber, image.DisplayOrder, image.Id
                    select new { image, teeth }
                ).ToListAsync(cancellationToken);

                var response = new List<PatientDentalPhotoModel>();
                foreach (var row in items)
                {
                    response.Add(new PatientDentalPhotoModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(row.image.Id, ProtectedIdPurpose.Patient),
                        PatientInfoId = await protectionProvider.EncryptIntIdAsync(row.teeth.PatientInfoId, ProtectedIdPurpose.Patient),
                        PatientTeethId = await protectionProvider.EncryptIntIdAsync(row.teeth.Id, ProtectedIdPurpose.Patient),
                        ToothNumber = row.teeth.ToothNumber,
                        Condition = row.teeth.Condition,
                        ToothRemarks = row.teeth.Remarks ?? string.Empty,
                        FileName = row.image.FileName ?? string.Empty,
                        FilePath = row.image.FilePath ?? string.Empty,
                        FileType = (int)row.image.FileType,
                        FileMediaType = row.image.FileMediaType ?? string.Empty,
                        FileExtension = row.image.FileExtension ?? string.Empty,
                        DisplayOrder = row.image.DisplayOrder,
                        Remarks = row.image.Remarks ?? string.Empty
                    });
                }

                return new SuccessResponse<List<PatientDentalPhotoModel>>(response);
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
