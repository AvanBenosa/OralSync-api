using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientDentalChart.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientDentalChart.Commands.Create
{
    [JsonSchema("CreatePatientDentalChartCommand")]
    public class Command : IRequest<Response>
    {
        public string PatientInfoId { get; set; } = string.Empty;
        public int? ToothNumber { get; set; }
        public DMD.DOMAIN.Enums.ToothCondition? Condition { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public List<PatientDentalChartSurfaceModel> Surfaces { get; set; } = new();
        public List<PatientDentalChartImageModel> Images { get; set; } = new();
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(DmdDbContext dbContext, IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PatientInfoId))
                {
                    return new BadRequestResponse("PatientInfoId is required.");
                }

                if (!request.ToothNumber.HasValue)
                {
                    return new BadRequestResponse("ToothNumber is required.");
                }

                var patientInfoId = await protectionProvider.DecryptIntIdAsync(
                    request.PatientInfoId,
                    ProtectedIdPurpose.Patient);

                var existing = await dbContext.PatientTeeth
                    .FirstOrDefaultAsync(
                        x => x.PatientInfoId == patientInfoId && x.ToothNumber == request.ToothNumber.Value,
                        cancellationToken);

                if (existing != null)
                {
                    return new BadRequestResponse("A dental chart record already exists for this tooth.");
                }

                var newItem = new PatientTeeth
                {
                    PatientInfoId = patientInfoId,
                    ToothNumber = request.ToothNumber.Value,
                    Condition = request.Condition ?? DMD.DOMAIN.Enums.ToothCondition.Healthy,
                    Remarks = request.Remarks ?? string.Empty
                };

                dbContext.PatientTeeth.Add(newItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                var surfaceModels = new List<PatientDentalChartSurfaceModel>();
                foreach (var surface in request.Surfaces ?? Enumerable.Empty<PatientDentalChartSurfaceModel>())
                {
                    if (!surface.Surface.HasValue || surface.Surface == DMD.DOMAIN.Enums.TeethSurface.None)
                    {
                        continue;
                    }

                    var surfaceEntity = new PatientTeethSurface
                    {
                        PatientTeethId = newItem.Id,
                        Surface = surface.Surface.Value,
                        TeethSurfaceName = string.IsNullOrWhiteSpace(surface.TeethSurfaceName)
                            ? surface.Surface.Value.ToString()
                            : surface.TeethSurfaceName,
                        Remarks = surface.Remarks ?? string.Empty
                    };

                    dbContext.PatientTeethSurface.Add(surfaceEntity);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    surfaceModels.Add(new PatientDentalChartSurfaceModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(surfaceEntity.Id, ProtectedIdPurpose.Patient),
                        PatientTeethId = await protectionProvider.EncryptIntIdAsync(newItem.Id, ProtectedIdPurpose.Patient),
                        Surface = surfaceEntity.Surface,
                        TeethSurfaceName = surfaceEntity.TeethSurfaceName,
                        Remarks = surfaceEntity.Remarks
                    });
                }

                var imageModels = new List<PatientDentalChartImageModel>();
                foreach (var image in (request.Images ?? Enumerable.Empty<PatientDentalChartImageModel>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.FilePath))
                    .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
                    .Take(3))
                {
                    var imageEntity = new PatientTeethImage
                    {
                        PatientTeethId = newItem.Id,
                        FileName = image.FileName ?? string.Empty,
                        OriginalFileName = string.IsNullOrWhiteSpace(image.OriginalFileName)
                            ? image.FileName ?? string.Empty
                            : image.OriginalFileName,
                        FilePath = image.FilePath ?? string.Empty,
                        FileType = image.FileType.HasValue
                            ? (FileType)image.FileType.Value
                            : FileType.Image,
                        FileMediaType = image.FileMediaType ?? string.Empty,
                        FileExtension = image.FileExtension ?? string.Empty,
                        DisplayOrder = image.DisplayOrder ?? (imageModels.Count + 1),
                        Remarks = image.Remarks ?? string.Empty
                    };

                    dbContext.PatientTeethImages.Add(imageEntity);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    imageModels.Add(new PatientDentalChartImageModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(imageEntity.Id, ProtectedIdPurpose.Patient),
                        PatientTeethId = await protectionProvider.EncryptIntIdAsync(newItem.Id, ProtectedIdPurpose.Patient),
                        FileName = imageEntity.FileName,
                        OriginalFileName = imageEntity.OriginalFileName,
                        FilePath = imageEntity.FilePath,
                        FileType = imageEntity.FileType,
                        FileMediaType = imageEntity.FileMediaType,
                        FileExtension = imageEntity.FileExtension,
                        DisplayOrder = imageEntity.DisplayOrder,
                        Remarks = imageEntity.Remarks
                    });
                }

                var response = new PatientDentalChartModel
                {
                    Id = await protectionProvider.EncryptIntIdAsync(newItem.Id, ProtectedIdPurpose.Patient),
                    PatientInfoId = await protectionProvider.EncryptIntIdAsync(newItem.PatientInfoId, ProtectedIdPurpose.Patient),
                    ToothNumber = newItem.ToothNumber,
                    Condition = newItem.Condition,
                    Remarks = newItem.Remarks,
                    Surfaces = surfaceModels,
                    Images = imageModels
                };

                return new SuccessResponse<PatientDentalChartModel>(response);
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
