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

namespace DMD.APPLICATION.PatientsModule.PatientDentalChart.Commands.Update
{
    [JsonSchema("UpdatePatientDentalChartCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
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
                var itemId = await protectionProvider.DecryptIntIdAsync(request.Id, ProtectedIdPurpose.Patient);
                var patientInfoId = await protectionProvider.DecryptIntIdAsync(request.PatientInfoId, ProtectedIdPurpose.Patient);

                var item = await dbContext.PatientTeeth
                    .FirstOrDefaultAsync(x => x.Id == itemId && x.PatientInfoId == patientInfoId, cancellationToken);

                if (item == null)
                {
                    return new BadRequestResponse("Item may have been modified or removed.");
                }

                if (request.ToothNumber.HasValue && request.ToothNumber.Value != item.ToothNumber)
                {
                    var duplicate = await dbContext.PatientTeeth.AnyAsync(
                        x => x.PatientInfoId == patientInfoId
                            && x.ToothNumber == request.ToothNumber.Value
                            && x.Id != itemId,
                        cancellationToken);

                    if (duplicate)
                    {
                        return new BadRequestResponse("A dental chart record already exists for this tooth.");
                    }

                    item.ToothNumber = request.ToothNumber.Value;
                }

                item.Condition = request.Condition ?? item.Condition;
                item.Remarks = request.Remarks ?? string.Empty;

                var existingSurfaces = await dbContext.PatientTeethSurface
                    .Where(x => x.PatientTeethId == item.Id)
                    .ToListAsync(cancellationToken);

                if (existingSurfaces.Count > 0)
                {
                    dbContext.PatientTeethSurface.RemoveRange(existingSurfaces);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var existingImages = await dbContext.PatientTeethImages
                    .Where(x => x.PatientTeethId == item.Id)
                    .ToListAsync(cancellationToken);

                if (existingImages.Count > 0)
                {
                    dbContext.PatientTeethImages.RemoveRange(existingImages);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var responseSurfaces = new List<PatientDentalChartSurfaceModel>();
                foreach (var surface in request.Surfaces ?? Enumerable.Empty<PatientDentalChartSurfaceModel>())
                {
                    if (!surface.Surface.HasValue || surface.Surface == DMD.DOMAIN.Enums.TeethSurface.None)
                    {
                        continue;
                    }

                    var entity = new PatientTeethSurface
                    {
                        PatientTeethId = item.Id,
                        Surface = surface.Surface.Value,
                        TeethSurfaceName = string.IsNullOrWhiteSpace(surface.TeethSurfaceName)
                            ? surface.Surface.Value.ToString()
                            : surface.TeethSurfaceName,
                        Remarks = surface.Remarks ?? string.Empty
                    };

                    dbContext.PatientTeethSurface.Add(entity);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    responseSurfaces.Add(new PatientDentalChartSurfaceModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(entity.Id, ProtectedIdPurpose.Patient),
                        PatientTeethId = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient),
                        Surface = entity.Surface,
                        TeethSurfaceName = entity.TeethSurfaceName,
                        Remarks = entity.Remarks
                    });
                }

                var responseImages = new List<PatientDentalChartImageModel>();
                foreach (var image in (request.Images ?? Enumerable.Empty<PatientDentalChartImageModel>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.FilePath))
                    .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
                    .Take(3))
                {
                    var entity = new PatientTeethImage
                    {
                        PatientTeethId = item.Id,
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
                        DisplayOrder = image.DisplayOrder ?? (responseImages.Count + 1),
                        Remarks = image.Remarks ?? string.Empty
                    };

                    dbContext.PatientTeethImages.Add(entity);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    responseImages.Add(new PatientDentalChartImageModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(entity.Id, ProtectedIdPurpose.Patient),
                        PatientTeethId = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient),
                        FileName = entity.FileName,
                        OriginalFileName = entity.OriginalFileName,
                        FilePath = entity.FilePath,
                        FileType = entity.FileType,
                        FileMediaType = entity.FileMediaType,
                        FileExtension = entity.FileExtension,
                        DisplayOrder = entity.DisplayOrder,
                        Remarks = entity.Remarks
                    });
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new PatientDentalChartModel
                {
                    Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient),
                    PatientInfoId = await protectionProvider.EncryptIntIdAsync(item.PatientInfoId, ProtectedIdPurpose.Patient),
                    ToothNumber = item.ToothNumber,
                    Condition = item.Condition,
                    Remarks = item.Remarks,
                    Surfaces = responseSurfaces,
                    Images = responseImages
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
