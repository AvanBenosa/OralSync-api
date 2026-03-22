using DMD.HANGFIRE.AppointmentReminders;
using DMD.HANGFIRE.ClinicAutoLocks;
using DMD.HANGFIRE.Common;
using DMD.PERSISTENCE.Context;
using DMD.SERVICES.Email;
using DMD.SERVICES.Sms;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDmdEmailServices(builder.Configuration);
builder.Services.AddDmdSmsServices(builder.Configuration);
builder.Services.AddDbContext<DmdDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("Default");
    options.UseSqlServer(connStr);
});
builder.Services
    .AddOptions<AppointmentReminderSettings>()
    .Bind(builder.Configuration.GetSection(AppointmentReminderSettings.SectionName))
    .Validate(
        static settings => !settings.IsEnabled || !string.IsNullOrWhiteSpace(settings.MorningCronExpression),
        "AppointmentReminderSettings:MorningCronExpression is required when appointment reminders are enabled.")
    .Validate(
        static settings => !settings.IsEnabled || !string.IsNullOrWhiteSpace(settings.EveningCronExpression),
        "AppointmentReminderSettings:EveningCronExpression is required when appointment reminders are enabled.")
    .ValidateOnStart();
builder.Services
    .AddOptions<ClinicAutoLockSettings>()
    .Bind(builder.Configuration.GetSection(ClinicAutoLockSettings.SectionName))
    .Validate(
        static settings => !settings.IsEnabled || !string.IsNullOrWhiteSpace(settings.CronExpression),
        "ClinicAutoLockSettings:CronExpression is required when clinic auto-lock is enabled.")
    .ValidateOnStart();
builder.Services.AddScoped<IAppointmentReminderJob, AppointmentReminderJob>();
builder.Services.AddScoped<IClinicAutoLockJob, ClinicAutoLockJob>();

builder.Services.AddHangfire((serviceProvider, configuration) =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("Hangfire")
            ?? builder.Configuration.GetConnectionString("Default"),
            new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true
            });
});

builder.Services.AddHangfireServer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var reminderSettings = scope.ServiceProvider
        .GetRequiredService<IOptions<AppointmentReminderSettings>>()
        .Value;
    var clinicAutoLockSettings = scope.ServiceProvider
        .GetRequiredService<IOptions<ClinicAutoLockSettings>>()
        .Value;

    const string legacyAppointmentReminderJobId = "patient-appointment-sms-reminders";
    const string eveningAppointmentReminderJobId = "patient-appointment-sms-reminders-evening-before";
    const string morningAppointmentReminderJobId = "patient-appointment-sms-reminders-morning-of";
    const string clinicAutoLockJobId = "clinic-profile-auto-lock-expired-validity";

    if (reminderSettings.IsEnabled)
    {
        var timeZone = HangfireTimeZoneResolver.Resolve(
            reminderSettings.TimeZoneId,
            AppointmentReminderSettings.SectionName);

        recurringJobManager.RemoveIfExists(legacyAppointmentReminderJobId);
        recurringJobManager.AddOrUpdate<IAppointmentReminderJob>(
            eveningAppointmentReminderJobId,
            job => job.SendEveningBeforeAppointmentRemindersAsync(CancellationToken.None),
            reminderSettings.EveningCronExpression,
            new RecurringJobOptions
            {
                TimeZone = timeZone
            });

        recurringJobManager.AddOrUpdate<IAppointmentReminderJob>(
            morningAppointmentReminderJobId,
            job => job.SendMorningOfAppointmentRemindersAsync(CancellationToken.None),
            reminderSettings.MorningCronExpression,
            new RecurringJobOptions
            {
                TimeZone = timeZone
            });

        app.Logger.LogInformation(
            "Registered recurring appointment reminder jobs '{EveningJobId}' and '{MorningJobId}' with crons '{EveningCronExpression}' and '{MorningCronExpression}' in timezone '{TimeZoneId}'.",
            eveningAppointmentReminderJobId,
            morningAppointmentReminderJobId,
            reminderSettings.EveningCronExpression,
            reminderSettings.MorningCronExpression,
            timeZone.Id);
    }
    else
    {
        recurringJobManager.RemoveIfExists(legacyAppointmentReminderJobId);
        recurringJobManager.RemoveIfExists(eveningAppointmentReminderJobId);
        recurringJobManager.RemoveIfExists(morningAppointmentReminderJobId);
        app.Logger.LogInformation(
            "Appointment reminder jobs are disabled and have been removed if they existed.");
    }

    if (clinicAutoLockSettings.IsEnabled)
    {
        var clinicAutoLockTimeZone = HangfireTimeZoneResolver.Resolve(
            clinicAutoLockSettings.TimeZoneId,
            ClinicAutoLockSettings.SectionName);

        recurringJobManager.AddOrUpdate<IClinicAutoLockJob>(
            clinicAutoLockJobId,
            job => job.AutoLockExpiredClinicsAsync(CancellationToken.None),
            clinicAutoLockSettings.CronExpression,
            new RecurringJobOptions
            {
                TimeZone = clinicAutoLockTimeZone
            });

        app.Logger.LogInformation(
            "Registered recurring clinic auto-lock job '{JobId}' with cron '{CronExpression}' in timezone '{TimeZoneId}'.",
            clinicAutoLockJobId,
            clinicAutoLockSettings.CronExpression,
            clinicAutoLockTimeZone.Id);
    }
    else
    {
        recurringJobManager.RemoveIfExists(clinicAutoLockJobId);
        app.Logger.LogInformation(
            "Clinic auto-lock job is disabled and has been removed if it existed.");
    }
}

var hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire")
    ?? builder.Configuration.GetConnectionString("Default");

if (!string.IsNullOrWhiteSpace(hangfireConnectionString))
{
    var hangfireBuilder = new SqlConnectionStringBuilder(hangfireConnectionString);
    var hangfireDatabaseName = hangfireBuilder.InitialCatalog;

    if (!string.IsNullOrWhiteSpace(hangfireDatabaseName))
    {
        var masterBuilder = new SqlConnectionStringBuilder(hangfireConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID(@databaseName) IS NULL BEGIN CREATE DATABASE [{hangfireDatabaseName}] END";
        command.Parameters.AddWithValue("@databaseName", hangfireDatabaseName);

        await command.ExecuteNonQueryAsync();
    }
}

app.UseRouting();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "DMD Hangfire Dashboard",
    Authorization = Array.Empty<IDashboardAuthorizationFilter>(),
    IgnoreAntiforgeryToken = true,
    IsReadOnlyFunc = _ => false
});

app.MapGet("/", () => Results.Redirect("/hangfire"));
app.MapGet("/health", () => Results.Ok("DMD Hangfire server is running."));

app.Run();
