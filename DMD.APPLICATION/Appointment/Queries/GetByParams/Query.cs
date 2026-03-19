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

                var appointments = await dbContext.AppointmentRequests
                    .AsNoTracking()
                    .OrderByDescending(x => x.AppointmentDateFrom)
                    .ToListAsync(cancellationToken);

                var patientIds = appointments
                    .Select(x => x.PatientInfoId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .Select(int.Parse)
                    .ToList();

                var patients = await dbContext.PatientInfos
                    .AsNoTracking()
                    .Where(x => x.ClinicProfileId == clinicId.Value && patientIds.Contains(x.Id))
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

                var items = await Task.WhenAll(appointments
                    .Where(x => !string.IsNullOrWhiteSpace(x.PatientInfoId) && patientLookup.ContainsKey(x.PatientInfoId))
                    .Select(async x =>
                    {
                        patientLookup.TryGetValue(x.PatientInfoId ?? string.Empty, out var patient);

                        return new AppointmentModel
                        {
                            Id = await protectionProvider.EncryptIntIdAsync(x.Id, ProtectedIdPurpose.Appointment),
                            PatientInfoId = await protectionProvider.EncryptIntIdAsync(int.Parse(x.PatientInfoId ?? "0"), ProtectedIdPurpose.Patient),
                            AppointmentDateFrom = x.AppointmentDateFrom,
                            AppointmentDateTo = x.AppointmentDateTo,
                            ReasonForVisit = x.ReasonForVisit ?? string.Empty,
                            Status = x.Status.ToString(),
                            Remarks = x.Remarks ?? string.Empty,
                            AppointmentType = x.AppointmentType.ToString(),
                            PatientName = patient?.PatientName ?? string.Empty,
                            PatientNumber = patient?.PatientNumber ?? string.Empty
                        };
                    }));

                var response = new AppointmentResponseModel
                {
                    Items = items.ToList(),
                    PageStart = request.PageStart,
                    PageEnd = request.PageEnd,
                    TotalCount = items.Length
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
