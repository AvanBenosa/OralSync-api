using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.Patient.Commands.Delete
{
    [JsonSchema("DeleteCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
    }

    public class ComandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public ComandHandler(DmdDbContext dbContext, IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var patientId = await protectionProvider.DecryptIntIdAsync(request.Id, ProtectedIdPurpose.Patient);
                var item = await dbContext.PatientInfos
                    .FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);

                var progressNotes = await dbContext.PatientProgressNotes.AsNoTracking()
                    .AnyAsync(x => x.PatientInfoId == item.Id);

                var patientPhotos = await dbContext.PatientMedicalHistories.AsNoTracking()
                    .AnyAsync(x => x.PatientInfoId == item.Id);

                var dentalChart = await dbContext.PatientTeeth.AsNoTracking()
                    .AnyAsync(x => x.PatientInfoId == item.Id);

                var patientForms = await dbContext.PatientForms.AsNoTracking()
                    .AnyAsync(x => x.PatientInfoId == item.Id);

                var appointmentRequest = await dbContext.AppointmentRequests.AsNoTracking()
                    .AnyAsync(x => x.PatientInfoId == item.Id);

                if (progressNotes || patientPhotos || dentalChart || patientForms || appointmentRequest) 
                    return new BadRequestResponse("Cannot delete this record due to existing dependencies.");

                if (item == null) return new BadRequestResponse("Item may have been modified or removed.");

                dbContext.PatientInfos.Remove(item);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new SuccessResponse<bool>(true);
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
