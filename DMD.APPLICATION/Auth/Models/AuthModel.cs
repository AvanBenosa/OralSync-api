namespace DMD.APPLICATION.Auth.Models
{

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
    }

    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public string EmploymentType { get; set; } = string.Empty;
        public string Preffix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicAddress { get; set; } = string.Empty;
        public string ClinicEmailAddress { get; set; } = string.Empty;
        public string ClinicContactNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
        public bool RequiresRegistration { get; set; }
    }

    public class RegistrationStatusResponse
    {
        public bool RequiresRegistration { get; set; }
    }

    public class VerificationCodeResponse
    {
        public string Email { get; set; } = string.Empty;
        public int ExpiresInMinutes { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }
        public string? TimeZone { get; set; }
        public string? Avatar { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ClinicId { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleLabel { get; set; } = string.Empty;
        public bool IsDataPrivacyAccepted { get; set; }
        public bool IsLocked { get; set; }
        public string? Avatar { get; set; }
        public string? ContactNumber { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }

    }

}
