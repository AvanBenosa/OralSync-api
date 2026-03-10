using DMD.DOMAIN.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.Clinic.Employees.Models
{
    public class EmployeeModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string EmailAddress { get; set; }
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public Suffix Suffix { get; set; }
        public Preffix Preffix { get; set; }
        public string Religion { get; set; }
        public DateTime? StartDate { get; set; }
        public EmploymentType EmploymentType { get; set; }
        public byte[] ProfilePictureData { get; set; }
        public string ProfilePicPath { get; set; }

    }
}
