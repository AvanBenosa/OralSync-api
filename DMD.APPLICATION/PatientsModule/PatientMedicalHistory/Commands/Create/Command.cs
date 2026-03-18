using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {
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
                var patientInfoId = await protectionProvider.DecryptIntIdAsync(request.PatientInfoId, ProtectedIdPurpose.Patient);
                var newItem = new DOMAIN.Entities.Patients.PatientMedicalHistory
                {
                    PatientInfoId = patientInfoId,
                    Date = request.Date,
                    Remarks = request.Remarks,
                    Others = request.Others,
                    Q1 = request.Q1,
                    Q2 = request.Q2,
                    Q3 = request.Q3,
                    Q4 = request.Q4,
                    Q5 = request.Q5,
                    Q6 = request.Q6,
                    Q7 = request.Q7,
                    Q8 = request.Q8,
                    Q9 = request.Q9,
                    Q10Nursing = request.Q10Nursing,
                    Q10Pregnant = request.Q10Pregnant,
                    Q11 = request.Q11,
                    Q12 = request.Q12,
                    Q13 = request.Q13,
                };

                dbContext.PatientMedicalHistories.Add(newItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = mapper.Map<PatientMedicalHistoryModel>(newItem);
                response.Id = await protectionProvider.EncryptIntIdAsync(newItem.Id, ProtectedIdPurpose.Patient);
                response.PatientsInfoId = await protectionProvider.EncryptIntIdAsync(newItem.PatientInfoId, ProtectedIdPurpose.Patient);
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
