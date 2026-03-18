using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Commands.Update
{
    [JsonSchema("UpdateCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public bool Q1 { get; set; }
        public bool Q2 { get; set; }
        public bool Q3 { get; set; }
        public bool Q4 { get; set; }
        public bool Q5 { get; set; }
        public bool Q6 { get; set; }
        public bool Q7 { get; set; }
        public bool Q8 { get; set; }
        public bool Q9 { get; set; }
        public bool Q10Nursing { get; set; }
        public bool Q10Pregnant { get; set; }
        public bool Q11 { get; set; }
        public bool Q12 { get; set; }
        public bool Q13 { get; set; }
        public string Others { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
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
                var itemId = await protectionProvider.DecryptIntIdAsync(request.Id, ProtectedIdPurpose.Patient);
                var patientInfoId = await protectionProvider.DecryptIntIdAsync(request.PatientInfoId, ProtectedIdPurpose.Patient);
                var item = await dbContext.PatientMedicalHistories.FirstOrDefaultAsync(
                    x => x.Id == itemId && x.PatientInfoId == patientInfoId,
                    cancellationToken);

                if (item == null)
                    return new BadRequestResponse("Item may have been modified or removed.");

                item.Date = request.Date;
                item.Q1 = request.Q1;
                item.Q2 = request.Q2;
                item.Q3 = request.Q3;
                item.Q4 = request.Q4;
                item.Q5 = request.Q5;
                item.Q6 = request.Q6;
                item.Q7 = request.Q7;
                item.Q8 = request.Q8;
                item.Q9 = request.Q9;
                item.Q10Nursing = request.Q10Nursing;
                item.Q10Pregnant = request.Q10Pregnant;
                item.Q11 = request.Q11;
                item.Q12 = request.Q12;
                item.Q13 = request.Q13;
                item.Others = request.Others;
                item.Remarks = request.Remarks;

                await dbContext.SaveChangesAsync(cancellationToken);

                var response = mapper.Map<PatientMedicalHistoryModel>(item);
                response.Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient);
                response.PatientsInfoId = await protectionProvider.EncryptIntIdAsync(item.PatientInfoId, ProtectedIdPurpose.Patient);
                return new SuccessResponse<PatientMedicalHistoryModel>(response);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
