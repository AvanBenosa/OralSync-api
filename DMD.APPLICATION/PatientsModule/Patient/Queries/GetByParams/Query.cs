using AutoMapper;
using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.Patient.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string ClinicId { get; set; }
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

                var response = new PatientResponseModel
                {
                    Items = new List<PatientModel>(),
                    PageEnd = request.PageEnd,
                    PageStart = request.PageStart
                };

                var items = await dbContext.PatientInfos.AsNoTracking()
                    .ToListAsync();

                if (items.Any())
                {
                    items.ForEach(x =>
                    {
                        var item = mapper.Map<PatientModel>(x);
                        response.Items.Add(item);
                    });
                }

                return new SuccessResponse<PatientResponseModel>(response);

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
