using DMD.DOMAIN.Entities.Appointment;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Entities.UserProfile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Claims;

namespace DMD.PERSISTENCE.Context
{
    public partial class DmdDbContext : IdentityDbContext<UserProfile, IdentityRole, string>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DmdDbContext(
            DbContextOptions<DmdDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null
        ) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ClinicProfile>()
                .Property(item => item.WorkingDays)
                .HasConversion(
                    new ValueConverter<List<string>, string>(
                        value => string.Join('|', value ?? new List<string>()),
                        value => string.IsNullOrWhiteSpace(value)
                            ? new List<string>()
                            : value.Split('|', StringSplitOptions.None).ToList()))
                .HasColumnType("nvarchar(max)");

            builder.Entity<ClinicProfile>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter
                    || (CurrentClinicId.HasValue && item.Id == CurrentClinicId.GetValueOrDefault()));

            builder.Entity<PatientInfo>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter
                    || (CurrentClinicId.HasValue
                        && item.ClinicProfileId == CurrentClinicId.GetValueOrDefault()));

            builder.Entity<PatientOverview>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientProgressNote>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientMedicalHistory>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientEmergencyContact>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientUploads>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientForm>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientTeeth>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientTeethSurface>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientTeeth.Any(teeth => teeth.Id == item.PatientTeethId)));

            builder.Entity<AppointmentRequest>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id.ToString() == item.PatientInfoId)));
        }

        private bool ShouldBypassClinicFilter =>
            _httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated != true;

        private int? CurrentClinicId
        {
            get
            {
                var clinicIdValue = _httpContextAccessor?.HttpContext?.User.FindFirstValue("clinicId");
                if (string.IsNullOrWhiteSpace(clinicIdValue))
                {
                    return null;
                }

                return int.TryParse(clinicIdValue, out var clinicId) ? clinicId : null;
            }
        }
    }
}
