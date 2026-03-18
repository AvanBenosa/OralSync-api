namespace DMD.APPLICATION.UserProfileModule.Models
{
    public class UserProfileModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Suffix { get; set; }
        public int Preffix { get; set; }
        public string Religion { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public int EmploymentType { get; set; }
        public string Bio { get; set; } = string.Empty;
        public int Role { get; set; }
        public string RoleLabel { get; set; } = string.Empty;
        public string? ClinicId { get; set; }
        public bool IsActive { get; set; }
    }
}
