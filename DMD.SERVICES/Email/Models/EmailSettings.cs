using System.Net;

namespace DMD.SERVICES.Email.Models
{
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool UseDefaultCredentials { get; set; }
        public int TimeoutMilliseconds { get; set; } = 100000;

        public ICredentialsByHost? BuildCredentials()
        {
            if (UseDefaultCredentials)
            {
                return CredentialCache.DefaultNetworkCredentials;
            }

            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                return null;
            }

            return new NetworkCredential(UserName, Password);
        }
    }
}
