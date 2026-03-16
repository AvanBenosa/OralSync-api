using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientOverview.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.PatientsModule.PatientOverview.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public int Id { get; set;  }
        public int PatientInfoId { get; set; }
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
                var response = new List<PatientOverviewModel>();
                var items = await dbContext.PatientOverviews.AsNoTracking()
                    .Where(x => x.PatientInfoId == request.PatientInfoId)
                    .Select(x => mapper.Map<PatientOverviewModel>(x))
                    .ToListAsync();

                if (items.Any())
                {
                    items.ForEach(x =>
                    {
                        var item = mapper.Map<PatientOverviewModel>(x);
                        response.Add(item);
                    });
                }

                return new SuccessResponse<List<PatientOverviewModel>>(response);

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
