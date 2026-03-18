using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientProgressNotes.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientProgressNotes.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {
        public string PatientInfoId { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Procedure { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public double Balance { get; set; }
        public string Account { get; set; } = string.Empty;
        public double Amount { get; set; }
        public double Discount { get; set; }
        public double TotalAmountDue { get; set; }
        public double AmountPaid { get; set; }
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
                var newItem = new DOMAIN.Entities.Patients.PatientProgressNote
                {
                    PatientInfoId = patientInfoId,
                    Date = request.Date,
                    Procedure = request.Procedure,
                    Balance = request.Balance,
                    Amount = request.Amount,
                    TotalAmountDue = request.TotalAmountDue,
                    Remarks = request.Remarks,
                    Category = request.Category,
                    Account = request.Account,
                    Discount = request.Discount,
                    AmountPaid = request.AmountPaid,
                };

                dbContext.PatientProgressNotes.Add(newItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = mapper.Map<PatientProgressNoteModel>(newItem);
                response.Id = await protectionProvider.EncryptIntIdAsync(newItem.Id, ProtectedIdPurpose.Patient);
                response.PatientInfoId = await protectionProvider.EncryptIntIdAsync(newItem.PatientInfoId, ProtectedIdPurpose.Patient);
                return new SuccessResponse<PatientProgressNoteModel>(response);
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
