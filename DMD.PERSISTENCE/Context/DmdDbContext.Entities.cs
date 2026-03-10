using DMD.DOMAIN.Entities;
using DMD.DOMAIN.Entities.Appointment;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Entities.UserProfile;
using Microsoft.EntityFrameworkCore;

namespace DMD.PERSISTENCE.Context
{
    public partial class DmdDbContext
    {
        public DbSet<UserProfile> UserProfiles { get;set; }

        public DbSet<PatientInfo> PatientInfos { get;set;  }

        public DbSet<PatientMedicalHistory> PatientMedicalHistories { get;set;  }

        public DbSet<PatientEmergencyContact> PatientEmergencyContacts { get; set; }    

        public DbSet<PatientUploads>PatientUploads { get; set; }

        public DbSet<PatientForm> PatientForms { get; set; }

        public DbSet<PatientTeeth> PatientTeeth { get; set; }
        public DbSet<PatientTeethSurface> PatientTeethSurface { get; set; }

        public DbSet<AppointmentRequest> AppointmentRequests { get; set; }

        public DbSet<AppointmentRemarks> AppointmentRemarks { get; set;  }
    }
}
