using static DMD.API.Configurations.Swash;
using static DMD.API.Configurations.Database;
using static DMD.API.Configurations.CORS;
using static DMD.API.Configurations.Mediator;
using static DMD.API.Configurations.Endpoints;
using static DMD.API.Configurations.Services;
using static DMD.API.Configurations.Identity;
using static DMD.API.Configurations.Authentication;
using DMD.API.Configurations;
using Hangfire;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

//Security
AddCorsPolicy(builder);
AddAuthentication(builder);


//Services
RegisterSwash(builder);
RegisterDatabase(builder);
RegisterMediatr(builder);
RegisterAutoMapper(builder);
AddFluentValidation(builder);
RegisterIdentity(builder);
RegisterServices(builder);
builder.Services.AddHangfire((serviceProvider, configuration) =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("Hangfire")
            ?? builder.Configuration.GetConnectionString("Default"));
});


// API 
AddControllers(builder);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;  // Remove "Server: Kestrel" header
});


var app = builder.Build();
UseSwagger(app);

var legacyUploadsRoot = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads");
Directory.CreateDirectory(Path.Combine(legacyUploadsRoot, "patients"));
Directory.CreateDirectory(Path.Combine(legacyUploadsRoot, "clinics"));

var configuredStorageRoot = builder.Configuration["Storage:RootPath"];
var storageRoot = string.IsNullOrWhiteSpace(configuredStorageRoot)
    ? Path.Combine(app.Environment.ContentRootPath, "storage")
    : Path.IsPathRooted(configuredStorageRoot)
        ? configuredStorageRoot
        : Path.Combine(app.Environment.ContentRootPath, configuredStorageRoot);

Directory.CreateDirectory(storageRoot);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(legacyUploadsRoot),
    RequestPath = "/uploads"
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
    await next();
});

app.UseMiddleware<ParameterPollutionMiddleware>();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

ApplyPendingMigrations(app);
ConfigureEndpoints(app);
ConfigureCors(app);

await ConfigureDatabase(services);

ConfigureAuthentication(app);
app.MapGet("/health", () => Results.Ok("DMD API is running."));
app.MapControllers();

await SeedDatabase(app);
app.Run();
