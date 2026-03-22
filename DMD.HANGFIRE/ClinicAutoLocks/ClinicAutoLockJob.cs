using DMD.PERSISTENCE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMD.HANGFIRE.ClinicAutoLocks
{
    public class ClinicAutoLockJob : IClinicAutoLockJob
    {
        private readonly DmdDbContext dbContext;
        private readonly ClinicAutoLockSettings settings;
        private readonly ILogger<ClinicAutoLockJob> logger;

        public ClinicAutoLockJob(
            DmdDbContext dbContext,
            IOptions<ClinicAutoLockSettings> settings,
            ILogger<ClinicAutoLockJob> logger)
        {
            this.dbContext = dbContext;
            this.settings = settings.Value;
            this.logger = logger;
        }

        public async Task AutoLockExpiredClinicsAsync(CancellationToken cancellationToken = default)
        {
            if (!settings.IsEnabled)
            {
                logger.LogInformation("Clinic auto-lock job is disabled. Skipping run.");
                return;
            }

            var timeZone = Common.HangfireTimeZoneResolver.Resolve(
                settings.TimeZoneId,
                ClinicAutoLockSettings.SectionName);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var localToday = localNow.Date;

            var clinicsToLock = await dbContext.ClinicProfiles
                .Where(clinic =>
                    !clinic.IsLocked &&
                    clinic.ValidityDate.Year > 1 &&
                    clinic.ValidityDate < localToday)
                .ToListAsync(cancellationToken);

            if (clinicsToLock.Count == 0)
            {
                logger.LogInformation(
                    "Clinic auto-lock job found no expired clinics to lock for local date {LocalDate}.",
                    localToday.ToString("yyyy-MM-dd"));
                return;
            }

            foreach (var clinic in clinicsToLock)
            {
                clinic.IsLocked = true;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Clinic auto-lock job locked {ClinicCount} clinic(s) for local date {LocalDate}. ClinicIds: {ClinicIds}",
                clinicsToLock.Count,
                localToday.ToString("yyyy-MM-dd"),
                string.Join(", ", clinicsToLock.Select(clinic => clinic.Id)));
        }
    }
}
