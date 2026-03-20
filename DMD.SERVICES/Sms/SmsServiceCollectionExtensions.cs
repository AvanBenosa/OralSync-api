using DMD.SERVICES.Sms.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMD.SERVICES.Sms
{
    public static class SmsServiceCollectionExtensions
    {
        public static IServiceCollection AddDmdSmsServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<SemaphoreSmsSettings>()
                .Bind(configuration.GetSection(SemaphoreSmsSettings.SectionName))
                .Validate(
                    static settings => settings.TimeoutMilliseconds > 0,
                    "SemaphoreSmsSettings:TimeoutMilliseconds must be greater than zero.")
                .Validate(
                    static settings => !settings.IsEnabled || !string.IsNullOrWhiteSpace(settings.ApiKey),
                    "SemaphoreSmsSettings:ApiKey is required when SMS is enabled.")
                .Validate(
                    static settings => !settings.IsEnabled || Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _),
                    "SemaphoreSmsSettings:BaseUrl must be a valid absolute URL when SMS is enabled.")
                .Validate(
                    static settings => !settings.IsEnabled || !string.IsNullOrWhiteSpace(settings.MessagesEndpoint),
                    "SemaphoreSmsSettings:MessagesEndpoint is required when SMS is enabled.")
                .Validate(
                    static settings => !settings.IsEnabled || !string.IsNullOrWhiteSpace(settings.PriorityEndpoint),
                    "SemaphoreSmsSettings:PriorityEndpoint is required when SMS is enabled.")
                .ValidateOnStart();

            services.AddHttpClient<ISmsService, SemaphoreSmsService>((serviceProvider, client) =>
            {
                var smsSettings = serviceProvider
                    .GetRequiredService<IOptions<SemaphoreSmsSettings>>()
                    .Value;

                var normalizedBaseUrl = smsSettings.BaseUrl?.Trim();
                if (!string.IsNullOrWhiteSpace(normalizedBaseUrl))
                {
                    normalizedBaseUrl = $"{normalizedBaseUrl.TrimEnd('/')}/";
                }

                if (Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri))
                {
                    client.BaseAddress = baseUri;
                }

                client.Timeout = TimeSpan.FromMilliseconds(smsSettings.TimeoutMilliseconds);
            });

            services.AddScoped<ISmsSenderJob, SmsSenderJob>();
            services.AddScoped<ISmsQueueService, SmsQueueService>();

            return services;
        }
    }
}
