using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace DMD.API.Configurations
{
    public static class Swash
    {
        internal static void RegisterSwash(WebApplicationBuilder builder)
        {
            var aspEnv = builder.Configuration.GetSection("ASPNETCORE_ENVIRONMENT")?.Value;

            if (aspEnv == "Development" || aspEnv == "Production" || aspEnv == "Test")
            {
                builder.Services.AddSwaggerGen(options =>
                {
                    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Description = "Copy 'Bearer ' + valid JWT token into field",
                        // Scheme = "Bearer",
                    });
                    options.OperationFilter<SecurityRequirementsOperationFilter>();
                    options.CustomSchemaIds(x => x.FullName);
                });
            }
        }

        internal static void UseSwagger(WebApplication app)
        {
            var aspEnv = app.Configuration.GetSection("ASPNETCORE_ENVIRONMENT")?.Value;
            if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local") || aspEnv == "Local" || aspEnv == "Test")
            {
                app.UseSwagger(options =>
                {
                    options.SerializeAsV2 = true;
                });

                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DMD API");
                    options.RoutePrefix = string.Empty;
                });
            }
        }
    }
}
