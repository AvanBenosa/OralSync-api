using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientProgressNotes.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientProgressNotes.Commands.Update
{
    [JsonSchema("UpdateCommand")]
    public class Command : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
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
                var itemId = await protectionProvider.DecryptIntIdAsync(request.Id, ProtectedIdPurpose.Patient);
                var patientInfoId = await protectionProvider.DecryptIntIdAsync(request.PatientInfoId, ProtectedIdPurpose.Patient);
                var item = await dbContext.PatientProgressNotes.FirstOrDefaultAsync(
                    x => x.Id == itemId && x.PatientInfoId == patientInfoId,
                    cancellationToken);

                if (item == null)
                    return new BadRequestResponse("Item may have been modified or removed.");

                item.Date = request.Date;
                item.Procedure = request.Procedure;
                item.Balance = request.Balance;
                item.AmountPaid = request.AmountPaid;
                item.Discount = request.Discount;
                item.TotalAmountDue = request.TotalAmountDue;
                item.Remarks = request.Remarks;
                item.Category = request.Category;
                item.Account = request.Account;
                item.Amount = request.Amount;

                await dbContext.SaveChangesAsync(cancellationToken);

                var response = mapper.Map<PatientProgressNoteModel>(item);
                response.Id = await protectionProvider.EncryptIntIdAsync(item.Id, ProtectedIdPurpose.Patient);
                response.PatientInfoId = await protectionProvider.EncryptIntIdAsync(item.PatientInfoId, ProtectedIdPurpose.Patient);
                return new SuccessResponse<PatientProgressNoteModel>(response);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
