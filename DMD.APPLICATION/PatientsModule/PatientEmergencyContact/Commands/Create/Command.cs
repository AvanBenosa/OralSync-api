using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Model;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using MediatR;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {
        public int PatientsInfoId { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }
        public Relationship Relationship { get; set; }
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
                var newItem = new DOMAIN.Entities.Patients.PatientEmergencyContact
                {
                    FullName = request.FullName,
                    Address = request.Address,
                    ContactNumber = request.ContactNumber,
                    EmailAddress = request.EmailAddress,
                    Relationship = request.Relationship
                };

                dbContext.PatientEmergencyContacts.Add(newItem);
                await dbContext.SaveChangesAsync();

                var response = mapper.Map<PatientEmergencyContactModel>(newItem);
                return new SuccessResponse<PatientEmergencyContactModel>(response);
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
