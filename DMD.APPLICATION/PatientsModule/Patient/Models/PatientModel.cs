using AutoMapper;
using DMD.DOMAIN.Entities.Patients;
using DMD.DOMAIN.Enums;
using System;

namespace DMD.APPLICATION.PatientsModule.Patient.Models
{
    public class PatientModel
    {
        public string Id { get; set; } = string.Empty;
        public string ClinicProfileId { get; set; } = string.Empty;
        public string PatientNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string EmailAddress { get; set; }
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public Suffix Suffix { get; set; }
        public string Occupation { get; set; }
        public string Religion { get; set; }
        public BloodTypes BloodType { get; set; }
        public CivilStatus CivilStatus { get; set; }
        public string ProfilePicture { get; set; }
    }
}
