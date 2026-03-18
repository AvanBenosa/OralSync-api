using DMD.SERVICES.Email.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DMD.SERVICES.Email
{
    public static class EmailServiceCollectionExtensions
    {
        public static IServiceCollection AddDmdEmailServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<EmailSettings>()
                .Bind(configuration.GetSection(EmailSettings.SectionName))
                .Validate(static settings => !string.IsNullOrWhiteSpace(settings.Host), "EmailSettings:Host is required.")
                .Validate(static settings => settings.Port > 0, "EmailSettings:Port must be greater than zero.")
                .Validate(static settings => !string.IsNullOrWhiteSpace(settings.FromEmail), "EmailSettings:FromEmail is required.")
                .Validate(static settings => settings.TimeoutMilliseconds > 0, "EmailSettings:TimeoutMilliseconds must be greater than zero.")
                .Validate(
                    static settings => settings.UseDefaultCredentials
                        || !string.IsNullOrWhiteSpace(settings.UserName),
                    "EmailSettings:UserName is required when UseDefaultCredentials is false.")
                .Validate(
                    static settings => settings.UseDefaultCredentials
                        || !string.IsNullOrWhiteSpace(settings.Password),
                    "EmailSettings:Password is required when UseDefaultCredentials is false.")
                .ValidateOnStart();

            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<IEmailSenderJob, EmailSenderJob>();
            services.AddScoped<IEmailQueueService, EmailQueueService>();

            return services;
        }
    }
}
