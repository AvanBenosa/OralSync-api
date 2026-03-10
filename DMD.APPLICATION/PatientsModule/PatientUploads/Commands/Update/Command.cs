using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientUploads.Commands.Update
{
    [JsonSchema("UpdateCommand")]
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
        public int PatientInfoId { get; set; }
        public string Remarks { get; set; }
    }
    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IMapper mapper;

        public CommandHandler(DmdDbContext dbContext, IMapper mapper)
        {
            this.mapper = mapper;
            this.dbContext = dbContext;
        }
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var item = await dbContext.PatientUploads.FirstOrDefaultAsync(x => x.Id == request.Id);

                if (item == null)
                    return new BadRequestResponse("Item may have been modified or removed.");

                item.Remarks = request.Remarks;

                await dbContext.SaveChangesAsync();
                await dbContext.DisposeAsync();

                var response = mapper.Map<PatientMedicalHistoryModel>(item);

                return new SuccessResponse<PatientMedicalHistoryModel>(response);

            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
