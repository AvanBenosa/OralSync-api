using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.PatientsModule.Patient.Models
{
    public class PatientModel
    {
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
    }
}
