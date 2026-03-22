using DMD.DOMAIN.Entities.Appointment;
using DMD.DOMAIN.Entities.Buildups;
using DMD.DOMAIN.Entities.FInances;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Emit;
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
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter
                    || (CurrentClinicId.HasValue && item.Id == CurrentClinicId.GetValueOrDefault()));

            builder.Entity<PatientInfo>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter
                    || (CurrentClinicId.HasValue
                        && item.ClinicProfileId == CurrentClinicId.GetValueOrDefault()));

            builder.Entity<FormTemplate>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter
                    || (CurrentClinicId.HasValue
                        && item.ClinicProfileId == CurrentClinicId.GetValueOrDefault()));

            builder.Entity<DentalInventory>()
            .HasQueryFilter(item =>
                ShouldBypassClinicFilter
                || (CurrentClinicId.HasValue
                    && item.ClinicProfileId == CurrentClinicId.GetValueOrDefault()));

            builder.Entity<DentalInventory>(entity =>
            {
                // STOCK (can have fractions like ml, grams)
                entity.Property(x => x.QuantityOnHand).HasPrecision(18, 2);
                entity.Property(x => x.MinimumStockLevel).HasPrecision(18, 2);
                entity.Property(x => x.MaximumStockLevel).HasPrecision(18, 2);

                // COSTING (money values)
                entity.Property(x => x.UnitCost).HasPrecision(18, 2);
                entity.Property(x => x.SellingPrice).HasPrecision(18, 2);
                entity.Property(x => x.TotalValue).HasPrecision(18, 2);
            });

            builder.Entity<ClinicExpenses>()
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
                .HasMany(item => item.TeethSurfaces)
                .WithOne(item => item.PatientTeeth)
                .HasForeignKey(item => item.PatientTeethId);

            builder.Entity<PatientTeeth>()
                .HasMany(item => item.TeethImages)
                .WithOne(item => item.PatientTeeth)
                .HasForeignKey(item => item.PatientTeethId);

            builder.Entity<PatientTeeth>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));

            builder.Entity<PatientTeethImage>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientTeeth.Any(teeth => teeth.Id == item.PatientTeethId)));

            builder.Entity<PatientTeethSurface>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientTeeth.Any(teeth => teeth.Id == item.PatientTeethId)));

            builder.Entity<AppointmentRequest>()
                .HasQueryFilter(item =>
                    ShouldBypassClinicFilter || (CurrentClinicId.HasValue
                        && PatientInfos.Any(patient => patient.Id == item.PatientInfoId)));
        }

        private bool ShouldBypassClinicFilter =>
            _httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated != true
            || string.Equals(
                _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.Role),
                UserRole.SuperAdmin.ToString(),
                StringComparison.OrdinalIgnoreCase);

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


