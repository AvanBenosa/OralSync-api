using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Finances.InvoiceGenerator.Models;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.Security.Claims;

namespace DMD.APPLICATION.Finances.InvoiceGenerator.Queries.GetByParams
{
    [JsonSchema("GetInvoiceGeneratorByParamsQuery")]
    public class Query : IRequest<Response>
    {
        public string? ClinicId { get; set; }
        public string? PatientInfoId { get; set; }
        public DateTime? Date { get; set; }
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProtectionProvider protectionProvider;

        public QueryHandler(
            DmdDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.protectionProvider = protectionProvider;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var clinicId = await protectionProvider.DecryptNullableIntIdAsync(
                    request.ClinicId,
                    ProtectedIdPurpose.Clinic);

                if (!clinicId.HasValue)
                {
                    var clinicIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue("clinicId");
                    clinicId = int.TryParse(clinicIdValue, out var currentClinicId) ? currentClinicId : null;
                }

                if (!clinicId.HasValue)
                {
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                int? patientInfoId = null;
                if (!string.IsNullOrWhiteSpace(request.PatientInfoId))
                {
                    patientInfoId = await protectionProvider.DecryptIntIdAsync(
                        request.PatientInfoId,
                        ProtectedIdPurpose.Patient);
                }

                var itemsQuery =
                    from note in dbContext.PatientProgressNotes.AsNoTracking()
                    join patient in dbContext.PatientInfos.AsNoTracking()
                        on note.PatientInfoId equals patient.Id
                    where patient.ClinicProfileId == clinicId.Value
                    select new
                    {
                        Note = note,
                        Patient = patient
                    };

                if (patientInfoId.HasValue)
                {
                    itemsQuery = itemsQuery.Where(x => x.Note.PatientInfoId == patientInfoId.Value);
                }

                if (request.Date.HasValue)
                {
                    var selectedDate = request.Date.Value.Date;
                    var nextDate = selectedDate.AddDays(1);

                    itemsQuery = itemsQuery.Where(x =>
                        x.Note.Date.HasValue &&
                        x.Note.Date.Value >= selectedDate &&
                        x.Note.Date.Value < nextDate);
                }

                var items = await itemsQuery
                    .OrderByDescending(x => x.Note.Date)
                    .ThenByDescending(x => x.Note.Id)
                    .ToListAsync(cancellationToken);

                var responseItems = new List<InvoiceGeneratorModel>();

                foreach (var item in items)
                {
                    responseItems.Add(new InvoiceGeneratorModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(item.Note.Id, ProtectedIdPurpose.Patient) ?? string.Empty,
                        PatientInfoId = await protectionProvider.EncryptIntIdAsync(item.Note.PatientInfoId, ProtectedIdPurpose.Patient) ?? string.Empty,
                        PatientName = BuildPatientName(item.Patient.LastName, item.Patient.FirstName, item.Patient.MiddleName),
                        PatientNumber = item.Patient.PatientNumber ?? string.Empty,
                        Date = item.Note.Date,
                        Procedure = item.Note.Procedure ?? string.Empty,
                        TotalAmount = item.Note.Amount,
                        AmountPaid = item.Note.AmountPaid,
                        Balance = item.Note.Balance
                    });
                }

                return new SuccessResponse<List<InvoiceGeneratorModel>>(responseItems);
            }
            catch (Exception error)
            {
                return new BadRequestResponse(error.GetBaseException().Message);
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
