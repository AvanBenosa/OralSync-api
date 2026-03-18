using AutoMapper;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.Patient.Commands.Update
{
    [JsonSchema("UpdateCommand")]
    public class Command : IRequest<Response>
    {
        public int ClinicProfileId { get; set; }
        public int Id { get; set;  }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string EmailAddress { get; set; }
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public Suffix Suffix { get; set; }
        public string Occupation { get; set; }
        public string Religion { get; set; }
        public BloodTypes BloodType { get; set; }
        public CivilStatus CivilStatus { get; set; }
        public string ProfilePicture { get; set; }
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
                var item = await dbContext.PatientInfos.FirstOrDefaultAsync(x => x.Id == request.Id);

                if (item == null) return new BadRequestResponse("Item may have been modified or removed.");


                item.FirstName = request.FirstName;
                item.LastName = request.LastName;
                item.MiddleName = request.MiddleName;
                item.EmailAddress = request.EmailAddress;
                item.Occupation = request.Occupation;
                item.Religion = request.Religion;
                item.CivilStatus = request.CivilStatus;
                item.Suffix = request.Suffix;
                item.Address = request.Address;
                item.ContactNumber = request.ContactNumber;
                item.BirthDate = request.BirthDate;
                item.BloodType = request.BloodType;
                item.ProfilePicture = request.ProfilePicture;
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
