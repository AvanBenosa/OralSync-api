using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.Patient.Commands.Update
{
    [JsonSchema("UpdateCommand")]
    public class Command : IRequest<Response>
    {
        public string ClinicProfileId { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public Suffix Suffix { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public BloodTypes BloodType { get; set; }
        public CivilStatus CivilStatus { get; set; }
        public string ProfilePicture { get; set; } = string.Empty;
    }

    public class CommandHandler : IRequestHandler<Command, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IMapper mapper;
        private readonly IProtectionProvider protectionProvider;

        public CommandHandler(DmdDbContext dbContext, IMapper mapper, IProtectionProvider protectionProvider)
        {
            this.mapper = mapper;
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var patientId = await protectionProvider.DecryptIntIdAsync(request.Id, ProtectedIdPurpose.Patient);
                var item = await dbContext.PatientInfos.FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);

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
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = mapper.Map<PatientModel>(item);
                response.Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient);
                response.ClinicProfileId = await protectionProvider.EncryptIntIdAsync(item.ClinicProfileId, ProtectedIdPurpose.Clinic);
                return new SuccessResponse<PatientModel>(response);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
