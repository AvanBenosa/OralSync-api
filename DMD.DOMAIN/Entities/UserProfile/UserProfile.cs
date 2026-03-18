using DMD.DOMAIN.Enums;
using Microsoft.AspNetCore.Identity;

namespace DMD.DOMAIN.Entities.UserProfile
{
    public class UserProfile : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        public UserRole Role { get; set; } = UserRole.User;
        public string RoleLabel => Role.ToString().ToLowerInvariant();

        public string EmailAddress { get; set; }
        public DateTime? BirthDate { get; set; }

        public string ContactNumber { get; set; }
        public string Address { get; set; }

        public Suffix Suffix { get; set; }
        public Preffix Preffix { get; set; }

        public string Religion { get; set; }
        public DateTime? StartDate { get; set; }
        public EmploymentType EmploymentType { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; }
        public int? ClinicId { get; set; }
    }
}
