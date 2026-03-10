using AutoMapper;
using DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model;
using DMD.APPLICATION.PatientsModule.PatientUploads.Models;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using MediatR;
using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.PatientUploads.Commands.Create
{
    [JsonSchema("CreateCommand")]
    public class Command : IRequest<Response>
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public FileType FileType { get; set; }
        public string FileMediaType { get; set; }
        public string FileExtension { get; set; }
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
                var newItem = mapper.Map<DOMAIN.Entities.Patients.PatientUploads>(request);

                dbContext.PatientUploads.Add(newItem);
                await dbContext.SaveChangesAsync();

                var result = mapper.Map<PatientUploadModel>(newItem);
                return new SuccessResponse<PatientUploadModel>(result);
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
