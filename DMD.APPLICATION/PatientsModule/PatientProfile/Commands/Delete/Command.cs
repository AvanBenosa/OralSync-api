using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientProfile.Commands.Delete
{
    [JsonSchema("DeleteCommand")]
    public class Command : IRequest<Response>
    {
        public int PatientId { get; set; }
    }
    public class ComandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;


        public ComandHandler(DmdDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var item = await dbContext.PatientInfos.FirstOrDefaultAsync(x => x.Id == request.PatientId);

                if (item == null) return new BadRequestResponse("Item may have been modified or removed.");

                dbContext.PatientInfos.Remove(item);
                await dbContext.SaveChangesAsync();

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
