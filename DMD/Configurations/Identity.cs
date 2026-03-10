
using DMD.DOMAIN.Entities.UserProfile;
using DMD.PERSISTENCE.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DMD.API.Configurations
{
    public class Identity
    {
        internal static void RegisterIdentity(WebApplicationBuilder builder)
        {
            builder.Services.AddIdentity<UserProfile, IdentityRole>()
             .AddEntityFrameworkStores<DmdDbContext>()
             .AddDefaultTokenProviders();
        }

        internal static void AddAuthentication(WebApplicationBuilder builder)
        {
            var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key is missing in appsettings.json"));
            var aud = builder.Configuration["Jwt:Audience"] ?? throw new Exception("Audience is missing in appsettings.json");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidAudience = aud,
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var token = context.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", string.Empty);
                        var accountsUrl = builder.Configuration["TokenKey"]
                                        ?? throw new Exception("Token is missing in appsettings.json");

                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                        var postResponse = await httpClient.PostAsync($"{accountsUrl}/api/auth/validate", null);

                        if (!postResponse.IsSuccessStatusCode)
                        {
                            context.Fail("Token validation failed");
                            // Setting Response.StatusCode here doesn't work directly; OnChallenge will handle it.
                        }
                    },
                    OnChallenge = context =>
                    {
                        // Handle the response for unauthorized access
                        context.HandleResponse(); // Prevents the default behavior
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var responseObj = new
                        {
                            Message = "Unauthorized access. Please provide a valid token."
                        };
                        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseObj);
                        return context.Response.WriteAsync(responseJson);
                    }
                };
            });

        }
    }
}
