using DMD.APPLICATION.Appointment.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.Appointment.Queries.GetByParams
{
    [JsonSchema("AppointmentGetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string ClinicId { get; set; } = string.Empty;
        public string Que { get; set; } = string.Empty;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
    }

    public class QueryHandler : IRequestHandler<Query, Response>
    {
        private readonly DmdDbContext dbContext;
        private readonly IProtectionProvider protectionProvider;

        public QueryHandler(DmdDbContext dbContext, IProtectionProvider protectionProvider)
        {
            this.dbContext = dbContext;
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
                    return new BadRequestResponse("Authenticated clinic was not found.");
                }

                var patients = await dbContext.PatientInfos
                    .AsNoTracking()
                    .Where(x => x.ClinicProfileId == clinicId.Value)
                    .Select(x => new
                    {
                        x.Id,
                        x.PatientNumber,
                        x.FirstName,
                        x.LastName,
                        x.MiddleName
                    })
                    .ToListAsync(cancellationToken);

                var patientLookup = patients.ToDictionary(
                    x => x.Id.ToString(),
                    x =>
                    {
                        var givenNames = string.Join(" ", new[] { x.FirstName, x.MiddleName }
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Select(value => value.Trim()));

                        var patientName = string.IsNullOrWhiteSpace(x.LastName)
                            ? givenNames
                            : string.IsNullOrWhiteSpace(givenNames)
                                ? x.LastName
                                : $"{x.LastName}, {givenNames}";

                        return new
                        {
                            x.PatientNumber,
                            PatientName = patientName
                        };
                    });

                var clinicPatientIds = patientLookup.Keys.ToList();

                var appointmentsQuery = dbContext.AppointmentRequests
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.PatientInfoId) && clinicPatientIds.Contains(x.PatientInfoId));

                if (request.DateFrom.HasValue)
                {
                    var dateFrom = request.DateFrom.Value.Date;
                    appointmentsQuery = appointmentsQuery.Where(x => x.AppointmentDateFrom >= dateFrom);
                }

                if (request.DateTo.HasValue)
                {
                    var dateToExclusive = request.DateTo.Value.Date.AddDays(1);
                    appointmentsQuery = appointmentsQuery.Where(x => x.AppointmentDateFrom < dateToExclusive);
                }

                var appointments = await appointmentsQuery
                    .OrderByDescending(x => x.AppointmentDateFrom)
                    .ToListAsync(cancellationToken);

                var appointmentRows = appointments
                    .Where(x => !string.IsNullOrWhiteSpace(x.PatientInfoId) && patientLookup.ContainsKey(x.PatientInfoId))
                    .Select(x =>
                    {
                        patientLookup.TryGetValue(x.PatientInfoId ?? string.Empty, out var patient);

                        return new
                        {
                            Appointment = x,
                            PatientInfoId = x.PatientInfoId ?? string.Empty,
                            ReasonForVisit = x.ReasonForVisit ?? string.Empty,
                            Status = x.Status.ToString(),
                            Remarks = x.Remarks ?? string.Empty,
                            AppointmentType = x.AppointmentType.ToString(),
                            PatientName = patient?.PatientName ?? string.Empty,
                            PatientNumber = patient?.PatientNumber ?? string.Empty
                        };
                    })
                    .ToList();

                var keyword = request.Que?.Trim();
                if (!string.IsNullOrWhiteSpace(keyword) && !string.Equals(keyword, "all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedKeyword = keyword.ToLower();
                    appointmentRows = appointmentRows
                        .Where(x =>
                            (x.PatientName ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                            (x.PatientNumber ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                            (x.ReasonForVisit ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                            (x.Status ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                            (x.AppointmentType ?? string.Empty).ToLower().Contains(normalizedKeyword) ||
                            (x.Remarks ?? string.Empty).ToLower().Contains(normalizedKeyword))
                        .ToList();
                }

                var totalCount = appointmentRows.Count;
                var hasDateFilter = request.DateFrom.HasValue || request.DateTo.HasValue;
                var today = DateTime.Today;
                var summaryCount = hasDateFilter
                    ? totalCount
                    : appointmentRows.Count(x => x.Appointment.AppointmentDateFrom.Date == today);
                var pageStart = Math.Max(request.PageStart, 0);
                var pageSize = request.PageEnd > 0 ? request.PageEnd : 25;

                var pagedRows = appointmentRows
                    .Skip(pageStart)
                    .Take(pageSize)
                    .ToList();

                var items = await Task.WhenAll(pagedRows.Select(async x =>
                {
                    var patientInfoId = int.TryParse(x.PatientInfoId, out var parsedPatientInfoId)
                        ? parsedPatientInfoId
                        : 0;

                    return new AppointmentModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(x.Appointment.Id, ProtectedIdPurpose.Appointment),
                        PatientInfoId = patientInfoId > 0
                            ? await protectionProvider.EncryptIntIdAsync(patientInfoId, ProtectedIdPurpose.Patient)
                            : string.Empty,
                        AppointmentDateFrom = x.Appointment.AppointmentDateFrom,
                        AppointmentDateTo = x.Appointment.AppointmentDateTo,
                        ReasonForVisit = x.ReasonForVisit,
                        Status = x.Status,
                        Remarks = x.Remarks,
                        AppointmentType = x.AppointmentType,
                        PatientName = x.PatientName,
                        PatientNumber = x.PatientNumber
                    };
                }));

                var response = new AppointmentResponseModel
                {
                    Items = items.ToList(),
                    PageStart = pageStart,
                    PageEnd = pageSize,
                    TotalCount = totalCount,
                    SummaryCount = summaryCount,
                    HasDateFilter = hasDateFilter
                };

                return new SuccessResponse<AppointmentResponseModel>(response);
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
