using Microsoft.AspNetCore.HttpOverrides;

namespace DMD.API.Configurations
{
    public class CORS
    {
        internal static void AddCorsPolicy(WebApplicationBuilder builder)
        {
            var corsOptions = new CorsOptions();
            builder.Configuration.GetSection("Cors").Bind(corsOptions);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("default", policy =>
                {
                    string[] value = Array.Empty<string>();
                    policy
                                        .WithOrigins(corsOptions.AllowedOrigins?.ToArray() ?? value)
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowCredentials()
                                        .WithExposedHeaders("Authorization");
                });
            });
        }

        internal static void ConfigureCors(WebApplication app)
        {
            app.UseCors("default");
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
        }


        public class CorsOptions
        {
            public List<string> AllowedOrigins { get; set; }
        }
    }
}
