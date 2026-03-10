using AutoMapper;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Commands.Update
{
    [JsonSchema("UpdateCommand")]
    public class Command : IRequest<Response>
    {
        public int Id { get; set;  }
        public int PatientInfoId { get; set; }
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
                var item = await dbContext.PatientEmergencyContacts.FirstOrDefaultAsync(x => x.Id == request.Id);

                if (item == null) return new BadRequestResponse("Item may have been modified or removed.");

                item.FullName = request.FullName;
                item.ContactNumber = request.ContactNumber;
                item.Address = request.Address;
                item.Address = request.Address;
                item.ContactNumber = request.ContactNumber;

                await dbContext.SaveChangesAsync();
                await dbContext.DisposeAsync();

                var response = mapper.Map<PatientModel>(item);

                return new SuccessResponse<PatientModel>(response);

            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
