using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {
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
                var newItem = new DOMAIN.Entities.Patients.PatientMedicalHistory
                {
                    Remarks = request.Remarks,
                };

                dbContext.PatientMedicalHistories.Add(newItem);
                await dbContext.SaveChangesAsync();

                var response = mapper.Map<PatientMedicalHistoryModel>(newItem);
                return new SuccessResponse<PatientMedicalHistoryModel>(response);
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

