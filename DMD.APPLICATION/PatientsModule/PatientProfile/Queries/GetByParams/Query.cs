using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientForm.Models;
using DMD.APPLICATION.PatientsModule.PatientProfile.Model;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientProfile.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {

        public int PatientId { get; set; }
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
                var item = await dbContext.PatientInfos
                    .AsNoTracking()
                    .Where(x => x.Id == request.PatientId)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return new BadRequestResponse("Patient not found.");
                }

                var result = mapper.Map<PatientProfileModel>(item);
                return new SuccessResponse<PatientProfileModel>(result);

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
