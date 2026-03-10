using AutoMapper;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using MediatR;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.Patient.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {

        public string PatientNumber { get; set; }
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
                var newItem = new PatientInfo
                {
                    PatientNumber = request.PatientNumber,
                    FirstName =request.FirstName,
                    LastName = request.LastName,
                    MiddleName = request.MiddleName,
                    EmailAddress = request.EmailAddress,
                    Occupation = request.Occupation,
                    Religion = request.Religion,
                    CivilStatus = request.CivilStatus,
                    Suffix = request.Suffix,
                    Address = request.Address,
                    ContactNumber = request.ContactNumber,
                    BirthDate = request.BirthDate,
                    BloodType = request.BloodType,
                };

                dbContext.PatientInfos.Add(newItem);
                await dbContext.SaveChangesAsync();

                var response = mapper.Map<PatientModel>(newItem);
                return new SuccessResponse<PatientModel>(response);
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
