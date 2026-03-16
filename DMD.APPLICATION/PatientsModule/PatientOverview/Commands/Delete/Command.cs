using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientOverview.Commands.Delete
{
    [JsonSchema("DeleteCommand")]
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
        public int PatientInfoId { get; set; }
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
                var item = await dbContext.PatientOverviews
                    .FirstOrDefaultAsync(x => x.Id == request.Id && x.PatientInfoId == request.PatientInfoId);

                if (item == null) return new BadRequestResponse("Item may have been modified or removed.");

                dbContext.PatientOverviews.Remove(item);
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
