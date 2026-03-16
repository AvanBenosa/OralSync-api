using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.PatientsModule.PatientOverview.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientOverview.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {
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
                var newItem = new DOMAIN.Entities.Patients.PatientOverview
                {
                    PatientInfoId = request.PatientInfoId,
                    PaymentDate = request.PaymentDate,
                    Procedure = request.Procedure,
                    Balance = request.Balance,
                    PaidAmount = request.PaidAmount,
                    TotalAmount = request.TotalAmount,
                    Remarks = request.Remarks
                };

                dbContext.PatientOverviews.Add(newItem);
                await dbContext.SaveChangesAsync();

                var response = mapper.Map<PatientOverviewModel>(newItem);
                return new SuccessResponse<PatientOverviewModel>(response);
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
