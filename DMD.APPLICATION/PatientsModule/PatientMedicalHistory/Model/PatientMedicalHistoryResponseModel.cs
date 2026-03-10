
namespace DMD.APPLICATION.PatientsModule.PatientMedicalHistory.Model
{
    public class PatientMedicalHistoryResponseModel
    {
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public int TotalCount { get; set; }
        public List<PatientMedicalHistoryModel> Items { get; set; }
    }
}
