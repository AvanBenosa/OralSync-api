using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientDentalChart.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientDentalChart.Queries.GetByParams
{
    [JsonSchema("GetPatientDentalChartByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
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
                var patientInfoId = await protectionProvider.DecryptIntIdAsync(
                    request.PatientInfoId,
                    ProtectedIdPurpose.Patient);

                int? patientTeethId = null;
                if (!string.IsNullOrWhiteSpace(request.Id))
                {
                    patientTeethId = await protectionProvider.DecryptIntIdAsync(
                        request.Id,
                        ProtectedIdPurpose.Patient);
                }

                var teethQuery = dbContext.PatientTeeth
                    .AsNoTracking()
                    .Where(x => x.PatientInfoId == patientInfoId);

                if (patientTeethId.HasValue)
                {
                    teethQuery = teethQuery.Where(x => x.Id == patientTeethId.Value);
                }

                var teethItems = await teethQuery
                    .OrderBy(x => x.ToothNumber)
                    .ToListAsync(cancellationToken);

                var toothIds = teethItems.Select(x => x.Id).ToList();
                var surfaceItems = toothIds.Count == 0
                    ? new List<DMD.DOMAIN.Entities.Patients.PatientTeethSurface>()
                    : await dbContext.PatientTeethSurface
                        .AsNoTracking()
                        .Where(x => toothIds.Contains(x.PatientTeethId))
                        .OrderBy(x => x.Id)
                        .ToListAsync(cancellationToken);
                var imageItems = toothIds.Count == 0
                    ? new List<DMD.DOMAIN.Entities.Patients.PatientTeethImage>()
                    : await dbContext.PatientTeethImages
                        .AsNoTracking()
                        .Where(x => toothIds.Contains(x.PatientTeethId))
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.Id)
                        .ToListAsync(cancellationToken);

                var models = new List<PatientDentalChartModel>();
                foreach (var tooth in teethItems)
                {
                    var model = new PatientDentalChartModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(tooth.Id, ProtectedIdPurpose.Patient),
                        PatientInfoId = await protectionProvider.EncryptIntIdAsync(tooth.PatientInfoId, ProtectedIdPurpose.Patient),
                        ToothNumber = tooth.ToothNumber,
                        Condition = tooth.Condition,
                        Remarks = tooth.Remarks ?? string.Empty,
                        Surfaces = new List<PatientDentalChartSurfaceModel>(),
                        Images = new List<PatientDentalChartImageModel>()
                    };

                    var toothSurfaces = surfaceItems.Where(x => x.PatientTeethId == tooth.Id).ToList();
                    foreach (var surface in toothSurfaces)
                    {
                        model.Surfaces.Add(new PatientDentalChartSurfaceModel
                        {
                            Id = await protectionProvider.EncryptIntIdAsync(surface.Id, ProtectedIdPurpose.Patient),
                            PatientTeethId = model.Id,
                            Surface = surface.Surface,
                            TeethSurfaceName = surface.TeethSurfaceName ?? string.Empty,
                            Remarks = surface.Remarks ?? string.Empty
                        });
                    }

                    var toothImages = imageItems.Where(x => x.PatientTeethId == tooth.Id).ToList();
                    foreach (var image in toothImages)
                    {
                        model.Images.Add(new PatientDentalChartImageModel
                        {
                            Id = await protectionProvider.EncryptIntIdAsync(image.Id, ProtectedIdPurpose.Patient),
                            PatientTeethId = model.Id,
                            FileName = image.FileName ?? string.Empty,
                            OriginalFileName = image.OriginalFileName ?? string.Empty,
                            FilePath = image.FilePath ?? string.Empty,
                            FileType = image.FileType,
                            FileMediaType = image.FileMediaType ?? string.Empty,
                            FileExtension = image.FileExtension ?? string.Empty,
                            DisplayOrder = image.DisplayOrder,
                            Remarks = image.Remarks ?? string.Empty
                        });
                    }

                    models.Add(model);
                }

                return new SuccessResponse<List<PatientDentalChartModel>>(models);
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
