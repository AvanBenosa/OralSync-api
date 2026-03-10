using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.PatientsModule.PatientUploads.Models;
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

namespace DMD.APPLICATION.PatientsModule.PatientUploads.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {

        public int PatientsInfoId { get; set; }
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

                var response = new PatientUploadResponseModel
                {
                    Items = new List<PatientUploadModel>(),
                    PageEnd = request.PageEnd,
                    PageStart = request.PageStart
                };

                var items = await dbContext.PatientUploads.AsNoTracking()
                    .Where(x => x.PatientInfoId == request.PatientsInfoId)
                    .ToListAsync();

                if (items.Any())
                {
                    items.ForEach(x =>
                    {
                        var item = mapper.Map<PatientUploadModel>(x);
                        response.Items.Add(item);
                    });
                }

                return new SuccessResponse<PatientUploadResponseModel>(response);

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
