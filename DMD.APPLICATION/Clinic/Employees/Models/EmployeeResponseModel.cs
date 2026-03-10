using DMD.APPLICATION.PatientsModule.PatientForm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMD.APPLICATION.Clinic.Employees.Models
{
    public class EmployeeResponseModel
    {
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public int TotalCount { get; set; }
        public List<PatientFormModel> Items { get; set; }
    }
}
