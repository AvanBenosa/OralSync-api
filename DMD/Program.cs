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


// ==========================
// BUILD APPLICATION
// ==========================
var builder = WebApplication.CreateBuilder(args);


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

// ==========================
// BUILD APP PIPELINE
// ==========================
var app = builder.Build();

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
app.UseMiddleware<ParameterPollutionMiddleware>();

// ==========================
// 11. DATABASE INITIALIZATION
// ==========================
// Create scope for DI services
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

// Apply database migrations automatically
ApplyPendingMigrations(app);

// Configure API endpoints (minimal APIs or grouped routes)
ConfigureEndpoints(app);

// Enable CORS middleware
ConfigureCors(app);

// Ensure database is ready / seeded config
await ConfigureDatabase(services);

// ==========================
// 12. AUTHENTICATION PIPELINE
// ==========================
// Enable authentication & authorization middleware
ConfigureAuthentication(app);


// ==========================
// 13. ROUTES / ENDPOINTS
// ==========================
// Health check endpoint
app.MapGet("/health", () => Results.Ok("DMD API is running."));

// Map controller routes
app.MapControllers();

// Seed initial data (users, roles, etc.)
await SeedDatabase(app);

app.Run();