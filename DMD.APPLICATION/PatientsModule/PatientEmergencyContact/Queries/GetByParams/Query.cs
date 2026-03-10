using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Model;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
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

namespace DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string PatientsInfoId { get; set; }
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
                var response = new List<PatientEmergencyContactModel>();
                var items = await dbContext.PatientEmergencyContacts.AsNoTracking()
                    .Select(x => mapper.Map<PatientEmergencyContactModel>(x))
                    .ToListAsync();

                if (items.Any())
                {
                    items.ForEach(x =>
                    {
                        var item = mapper.Map<PatientEmergencyContactModel>(x);
                        response.Add(item);
                    });
                }

                return new SuccessResponse<List<PatientEmergencyContactModel>>(response);

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
