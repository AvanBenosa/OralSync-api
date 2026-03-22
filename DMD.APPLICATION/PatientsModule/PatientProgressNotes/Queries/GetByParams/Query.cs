using AutoMapper;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.PatientsModule.PatientProgressNotes.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.PatientsModule.PatientProgressNotes.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string Id { get; set; } = string.Empty;
        public string PatientInfoId { get; set; } = string.Empty;
        public string ClinicId { get; set; } = string.Empty;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly IMapper mapper;
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;
        private readonly IHttpContextAccessor httpContextAccessor;

        public QueryHandler(
            DmdDbContext dbContext,
            IMapper mapper,
            IProtectionProvider protectionProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            this.mapper = mapper;
            this.dbContext = dbContext;
            this.protectionProvider = protectionProvider;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                int? patientInfoId = null;
                if (!string.IsNullOrWhiteSpace(request.PatientInfoId))
                {
                    patientInfoId = await protectionProvider.DecryptIntIdAsync(
                        request.PatientInfoId,
                        ProtectedIdPurpose.Patient);
                }

                var clinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.ClinicId,
                    ProtectedIdPurpose.Clinic);

                if (!clinicId.HasValue)
                {
                    var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                    clinicId = int.TryParse(clinicIdValue, out var currentClinicId) ? currentClinicId : null;
                }

                var itemsQuery =
                    from note in dbContext.PatientProgressNotes.AsNoTracking()
                    join patient in dbContext.PatientInfos.AsNoTracking()
                        on note.PatientInfoId equals patient.Id
                    select new
                    {
                        Note = note,
                        Patient = patient
                    };

                if (patientInfoId.HasValue)
                {
                    itemsQuery = itemsQuery.Where(x => x.Note.PatientInfoId == patientInfoId.Value);
                }

                if (clinicId.HasValue)
                {
                    itemsQuery = itemsQuery.Where(x => x.Patient.ClinicProfileId == clinicId.Value);
                }

                if (request.DateFrom.HasValue)
                {
                    var dateFrom = request.DateFrom.Value.Date;
                    itemsQuery = itemsQuery.Where(x =>
                        x.Note.Date.HasValue &&
                        x.Note.Date.Value >= dateFrom);
                }

                if (request.DateTo.HasValue)
                {
                    var dateToExclusive = request.DateTo.Value.Date.AddDays(1);
                    itemsQuery = itemsQuery.Where(x =>
                        x.Note.Date.HasValue &&
                        x.Note.Date.Value < dateToExclusive);
                }

                var items = await itemsQuery
                    .AsNoTracking()
                    .OrderBy(x => x.Note.Date)
                    .ThenByDescending(x => x.Note.Id)
                    .ToListAsync(cancellationToken);

                var response = await Task.WhenAll(items.Select(async x =>
                {
                    var item = mapper.Map<PatientProgressNoteModel>(x.Note);
                    item.Id = await protectionProvider.EncryptIntIdAsync(
                        x.Note.Id,
                        ProtectedIdPurpose.Patient);
                    item.PatientInfoId = await protectionProvider.EncryptIntIdAsync(
                        x.Note.PatientInfoId,
                        ProtectedIdPurpose.Patient);
                    item.PatientNumber = x.Patient.PatientNumber ?? string.Empty;
                    item.PatientName = BuildPatientName(
                        x.Patient.LastName,
                        x.Patient.FirstName,
                        x.Patient.MiddleName);
                    return item;
                }));

                return new SuccessResponse<List<PatientProgressNoteModel>>(response.ToList());
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

        private static string BuildPatientName(
            string? lastName,
            string? firstName,
            string? middleName)
        {
            var givenNames = string.Join(" ", new[] { firstName, middleName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

            if (string.IsNullOrWhiteSpace(lastName))
            {
                return givenNames;
            }

            return string.IsNullOrWhiteSpace(givenNames)
                ? lastName.Trim()
                : $"{lastName.Trim()}, {givenNames}";
        }
    }
}
