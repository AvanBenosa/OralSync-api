using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientOverview.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientOverview.Commands.Update
{
    [JsonSchema("UpdateCommand")]
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
        public int PatientInfoId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Procedure { get; set; }
        public double Balance { get; set; }
        public double PaidAmount { get; set; }
        public double TotalAmount { get; set; }
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
                var item = await dbContext.PatientOverviews.FirstOrDefaultAsync(x => x.Id == request.Id && x.PatientInfoId == request.PatientInfoId);

                if (item == null)
                    return new BadRequestResponse("Item may have been modified or removed.");

                item.PaymentDate = request.PaymentDate;
                item.Procedure = request.Procedure;
                item.Balance = request.Balance;
                item.PaidAmount = request.PaidAmount;
                item.TotalAmount = request.TotalAmount;
                item.Remarks = request.Remarks;
                await dbContext.SaveChangesAsync();
                await dbContext.DisposeAsync();

                var response = mapper.Map<PatientOverviewModel>(item);

                return new SuccessResponse<PatientOverviewModel>(response);

            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
            }
        }
    }
}
