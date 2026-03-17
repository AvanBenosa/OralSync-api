using DMD.SERVICES.Email;
using DMD.SERVICES.Email.Models;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IEmailSenderJob, EmailSenderJob>();

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
