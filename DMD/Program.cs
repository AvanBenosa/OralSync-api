using static DMD.API.Configurations.Swash;
using static DMD.API.Configurations.Database;
using static DMD.API.Configurations.CORS;
using static DMD.API.Configurations.Mediator;
using static DMD.API.Configurations.Endpoints;
using static DMD.API.Configurations.Services;
using static DMD.API.Configurations.Identity;
using static DMD.API.Configurations.Authentication;
using DMD.API.Configurations;

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


// API 
AddControllers(builder);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;  // Remove "Server: Kestrel" header
});


var app = builder.Build();
UseSwagger(app);

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

await ConfigureDatabase(services);

ConfigureAuthentication(app);
app.MapControllers();

await SeedDatabase(app);
app.Run();
