using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Model;
using DMD.APPLICATION.PatientsModule.PatientForm.Models;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;


namespace DMD.APPLICATION.PatientsModule.PatientForm.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {

        public int PatientInfoId { get; set; }
        public string Que { get; set; }
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
    }
    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly IMapper mapper;
        private readonly DmdDbContext dbContext;

        public QueryHandler(DmdDbContext dbContext, IMapper mapper)
        {
            this.mapper = mapper;
            this.dbContext = dbContext;
        }
        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {

                var response = new PatientFormResposeModel
                {
                    Items = new List<PatientFormModel>(),
                    PageEnd = request.PageEnd,
                    PageStart = request.PageStart
                };

                var items = await dbContext.PatientForms.AsNoTracking()
                    .Where(x => x.PatientInfoId == request.PatientInfoId)
                    .ToListAsync();

                if (items.Any())
                {
                    items.ForEach(x =>
                    {
                        var item = mapper.Map<PatientFormModel>(x);
                        response.Items.Add(item);
                    });
                }

                return new SuccessResponse<PatientFormResposeModel>(response);

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
