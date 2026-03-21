using DMD.APPLICATION.Dashboard.Models;
using DMD.APPLICATION.Common.ProtectedIds;
using DMD.APPLICATION.Responses;
using DMD.DOMAIN.Enums.Appointment;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.ProtectionProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace DMD.APPLICATION.Dashboard.Queries.GetByParams
{
    [JsonSchema("GetByParamQuery")]
    public class Query : IRequest<Response>
    {
        public string? ClinicId { get; set; }
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
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var nextDay = tomorrow.AddDays(1);
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var nextMonthStart = monthStart.AddMonths(1);
                var yearStart = new DateTime(today.Year, 1, 1);
                var nextYearStart = yearStart.AddYears(1);

                var totalPatients = await dbContext.PatientInfos
                    .AsNoTracking()
                    .CountAsync(cancellationToken);

                var patientsToday = await dbContext.PatientInfos
                    .AsNoTracking()
                    .CountAsync(x => x.CreatedAt >= today && x.CreatedAt < tomorrow, cancellationToken);

                var scheduledAppointments = await dbContext.AppointmentRequests
                    .AsNoTracking()
                    .CountAsync(x => x.Status == AppointmentStatus.Scheduled, cancellationToken);

                var pendingAppointments = await dbContext.AppointmentRequests
                    .AsNoTracking()
                    .CountAsync(x => x.Status == AppointmentStatus.Pending, cancellationToken);

                var incomeToday = await dbContext.PatientProgressNotes
                    .AsNoTracking()
                    .Where(x => x.Date.HasValue && x.Date.Value == today)
                    .SumAsync(x => (double?)x.AmountPaid, cancellationToken) ?? 0;

                var totalIncomeMonthly = await dbContext.PatientProgressNotes
                    .AsNoTracking()
                    .Where(x => x.Date.HasValue && x.Date.Value >= monthStart && x.Date.Value < nextMonthStart)
                    .SumAsync(x => (double?)x.AmountPaid, cancellationToken) ?? 0;

                var totalExpenseMonthly = await dbContext.ClinicExpenses
                    .AsNoTracking()
                    .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
                    .SumAsync(x => (double?)x.Amount, cancellationToken) ?? 0;

                var latestPatientItems = await dbContext.PatientInfos
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(5)
                    .Select(x => new
                    {
                        x.Id,
                        x.PatientNumber,
                        x.FirstName,
                        x.MiddleName,
                        x.LastName,
                        LatestActivity = dbContext.PatientProgressNotes
                            .Where(note => note.PatientInfoId == x.Id)
                            .OrderByDescending(note => note.CreatedAt)
                            .Select(note => note.Procedure)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                var latestPatients = (await Task.WhenAll(latestPatientItems
                    .Select(async x => new DashboardPatientItemModel
                    {
                        Id = await protectionProvider.EncryptIntIdAsync(x.Id, ProtectedIdPurpose.Patient),
                        PatientNumber = x.PatientNumber,
                        FullName = BuildFullName(x.FirstName, x.MiddleName, x.LastName),
                        LatestActivity = string.IsNullOrWhiteSpace(x.LatestActivity) ? "Patient record created" : x.LatestActivity
                    }))).ToList();

                var todayAppointments = await BuildAppointmentList(today, tomorrow, cancellationToken);
                var nextDayAppointments = await BuildAppointmentList(tomorrow, nextDay, cancellationToken);

                var monthlyIncomeRaw = await dbContext.PatientProgressNotes
                    .AsNoTracking()
                    .Where(x => x.Date.HasValue && x.Date.Value >= yearStart && x.Date.Value < nextYearStart)
                    .GroupBy(x => x.Date!.Value.Month)
                    .Select(group => new
                    {
                        Month = group.Key,
                        Income = group.Sum(x => x.AmountPaid)
                    })
                    .ToListAsync(cancellationToken);

                var monthlyExpenseRaw = await dbContext.ClinicExpenses
                    .AsNoTracking()
                    .Where(x => x.Date >= yearStart && x.Date < nextYearStart)
                    .GroupBy(x => x.Date.Month)
                    .Select(group => new
                    {
                        Month = group.Key,
                        Expenses = group.Sum(x => x.Amount)
                    })
                    .ToListAsync(cancellationToken);

                var monthlyIncome = BuildMonthlyIncomeSeries(
                    monthlyIncomeRaw.ToDictionary(x => x.Month, x => x.Income),
                    monthlyExpenseRaw.ToDictionary(x => x.Month, x => x.Expenses),
                    today.Year);

                var monthlyRevenue = await dbContext.PatientProgressNotes
                    .AsNoTracking()
                    .Where(x => x.Date.HasValue && x.Date.Value >= monthStart && x.Date.Value < nextMonthStart)
                    .GroupBy(x => x.Category)
                    .Select(group => new MonthlyRevenueModel
                    {
                        Treatment = group.Key,
                        TotalAmount = group.Sum(x => x.AmountPaid)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                var response = new DashboardResponseModel
                {
                    TotalPatients = totalPatients,
                    PatientsToday = patientsToday,
                    ScheduledAppointments = scheduledAppointments,
                    PendingAppointments = pendingAppointments,
                    IncomeToday = incomeToday,
                    TotalIncomeMonthly = totalIncomeMonthly,
                    TotalExpenseMonthly = totalExpenseMonthly,
                    LatestPatients = latestPatients,
                    AddPatients = true,
                    AddAppointment = true,
                    MonthlyIncome = monthlyIncome,
                    MonthlyRevenue = monthlyRevenue,
                    NextDayAppointment = nextDayAppointments,
                    TodayAppointment = todayAppointments
                };

                return new SuccessResponse<DashboardResponseModel>(response);
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

        private async Task<List<DashboardAppointmentModel>> BuildAppointmentList(
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken)
        {
            var items = await dbContext.AppointmentRequests
                .AsNoTracking()
                .Where(x => x.AppointmentDateFrom >= start && x.AppointmentDateFrom < end)
                .OrderBy(x => x.AppointmentDateFrom)
                .Take(5)
                .Select(x => new
                {
                    x.AppointmentDateFrom,
                    x.PatientInfoId,
                    x.ReasonForVisit,
                    x.Status
                })
                .ToListAsync(cancellationToken);

            return items
                .Select(x => new DashboardAppointmentModel
                {
                    Time = x.AppointmentDateFrom.ToString("hh:mm tt"),
                    //FullName = x.PatientName,
                    Reason = x.ReasonForVisit,
                    Highlight = x.Status == AppointmentStatus.Scheduled
                })
                .ToList();
        }

        private static List<MonthlyIncomeModel> BuildMonthlyIncomeSeries(
            Dictionary<int, double> monthlyIncomeLookup,
            Dictionary<int, double> monthlyExpenseLookup,
            int year)
        {
            var items = new List<MonthlyIncomeModel>();

            for (var month = 1; month <= 12; month++)
            {
                items.Add(new MonthlyIncomeModel
                {
                    Month = new DateTime(year, month, 1).ToString("MMM"),
                    Income = monthlyIncomeLookup.TryGetValue(month, out var income) ? income : 0,
                    Expenses = monthlyExpenseLookup.TryGetValue(month, out var expenses)
                        ? expenses
                        : 0
                });
            }

            return items;
        }

        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            return string.Join(" ", new[] { firstName, middleName, lastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}
