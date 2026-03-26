using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace DMD.API.Configurations
{
    public static class Swash
    {
        internal static void RegisterSwash(WebApplicationBuilder builder)
        {
            if (ShouldEnableSwagger(builder.Environment))
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
            if (ShouldEnableSwagger(app.Environment))
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

        private static bool ShouldEnableSwagger(IHostEnvironment environment)
        {
            return environment.IsDevelopment()
                || environment.IsEnvironment("Local")
                || environment.IsEnvironment("Production");
        }
    }
}
