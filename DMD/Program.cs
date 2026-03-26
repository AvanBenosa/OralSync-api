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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


// ==========================
// BUILD APPLICATION
// ==========================
var builder = WebApplication.CreateBuilder(args);

// Enhanced logging for startup diagnostics
builder.Logging.AddConsole();



// ==========================
// 1. SECURITY (EARLY SETUP)
// ==========================
// Configure CORS policies (who can access your API)
AddCorsPolicy(builder);

// Configure authentication (JWT, cookies, etc.)
AddAuthentication(builder);

// ==========================
// 2. REGISTER CORE SERVICES
// ==========================
// Swagger (API documentation)
RegisterSwash(builder);

// Database (DbContext, connection string)
RegisterDatabase(builder);

// MediatR (CQRS pattern / request handling)
RegisterMediatr(builder);

// AutoMapper (object-to-object mapping)
RegisterAutoMapper(builder);

// FluentValidation (input validation)
AddFluentValidation(builder);

// Identity (user management, roles, auth system)
RegisterIdentity(builder);

// Custom services (business logic layer)
RegisterServices(builder);


// ==========================
// 3. BACKGROUND JOBS (HANGFIRE)
// ==========================
// Configure Hangfire for background processing
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


// ==========================
// 4. API CONTROLLERS
// ==========================
// Enable controllers (MVC endpoints)
AddControllers(builder);

// ==========================
// 5. SERVER CONFIGURATION
// ==========================
// Remove server header for security reasons
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

// Log environment and masked connection strings for diagnostics
var env = builder.Environment;
Console.WriteLine($"🚀 Startup Environment: {env.EnvironmentName}");
Console.WriteLine($"🚀 Configuration Sources Count: {builder.Configuration.Sources.Count()}");

var defaultConn = builder.Configuration.GetConnectionString("Default");
var hangfireConn = builder.Configuration.GetConnectionString("Hangfire");
Console.WriteLine($"🚀 Default Conn: {(defaultConn?.StartsWith("Server=") == true ? "✅ Configured" : "❌ Missing/Invalid")}");
Console.WriteLine($"🚀 Hangfire Conn: {(hangfireConn?.StartsWith("Server=") == true ? "✅ Configured" : "❌ Missing/Invalid")}");

// ==========================
// BUILD APP PIPELINE
// ==========================
var app = builder.Build();

var logger = app.Logger;
var isProduction = app.Environment.IsProduction() == true;
logger.LogInformation("✅ App built successfully. IsProduction: {IsProd}", isProduction);

// ==========================
// 7. DEVELOPMENT TOOLS
// ==========================
// Enable Swagger UI (API testing page)
UseSwagger(app);

// ==========================
// 9. CUSTOM SECURITY HEADERS (INLINE)
// ==========================
// Prevent iframe embedding (clickjacking protection)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
    await next();
});

// ==========================
// 10. CUSTOM MIDDLEWARE
// ==========================
// Prevent parameter pollution attacks
//app.UseMiddleware<ParameterPollutionMiddleware>();

// ==========================
///** 11. DATABASE INITIALIZATION (with validation) **
// ==========================
logger.LogInformation("🔄 Phase 11: Starting Database Initialization...");

try
{
    using var scope = app.Services.CreateScope();
    var scopedServices = scope.ServiceProvider;
    logger.LogInformation("✅ Scope created successfully.");

    if (!isProduction)
    {
        logger.LogInformation("🔄 [DEV ONLY] Calling ConfigureDatabase...");
        await ConfigureDatabase(scopedServices);

        logger.LogInformation("🔄 [DEV ONLY] Calling ApplyPendingMigrations...");
        ApplyPendingMigrations(app);

        logger.LogInformation("🔄 [DEV ONLY] Calling SeedDatabase...");
        await SeedDatabase(app);
        logger.LogInformation("✅ [DEV ONLY] Database setup complete.");
    }
    else
    {
        logger.LogWarning("⚠️  [PROD] Skipping automatic DB setup (migrations/seed). Run manually.");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "💥 FATAL: Database initialization failed at phase 11. Stack: {StackTrace}", ex.StackTrace);
    throw; // Re-throw to prevent app start if critical
}

// Configure API endpoints (minimal APIs or grouped routes)
ConfigureEndpoints(app);

// Enable CORS middleware
ConfigureCors(app);

// ==========================
// 12. AUTHENTICATION PIPELINE
// ==========================
// Enable authentication & authorization middleware
ConfigureAuthentication(app);


// ==========================
// 13. ROUTES / ENDPOINTS
// ==========================
// Enhanced health + startup log endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow, Environment = app.Environment.EnvironmentName }));

app.MapGet("/startup-log", () => Results.Ok(new {
    Environment = app.Environment.EnvironmentName,
    ConnStringsConfigured = app.Configuration.GetConnectionString("Default") != null,
    IsProduction = app.Environment.IsProduction(),
    Timestamp = DateTime.UtcNow
}));

// Map controller routes
app.MapControllers();

app.Run();