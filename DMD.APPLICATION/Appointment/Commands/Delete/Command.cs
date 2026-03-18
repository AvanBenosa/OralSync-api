using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.Appointment.Commands.Delete
{
    [JsonSchema("AppointmentDeleteCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
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
                var appointmentId = await protectionProvider.DecryptIntIdAsync(request.Id, ProtectedIdPurpose.Appointment);
                var item = await dbContext.AppointmentRequests
                    .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

                if (item == null)
                    return new BadRequestResponse("Item may have been modified or removed.");

                dbContext.AppointmentRequests.Remove(item);
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
