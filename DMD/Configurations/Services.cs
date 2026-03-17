using DMD.SERVICES;
using DMD.SERVICES.Email;
using DMD.SERVICES.Email.Models;

namespace DMD.API.Configurations
{
    public static class Services
    {
        internal static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<TokenService>();
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddScoped<IEmailSenderJob, EmailSenderJob>();
            builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
        }
    }
}
