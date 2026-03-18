
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
                        var validationBaseUrl = builder.Configuration["Auth:ValidationBaseUrl"]
                            ?? builder.Configuration["Accounts:BaseUrl"];

                        if (string.IsNullOrWhiteSpace(token))
                        {
                            context.Fail("Missing bearer token.");
                            return;
                        }

                        // Local JWT validation is already configured above. Only call an external
                        // validation endpoint when an absolute base URL is explicitly configured.
                        if (string.IsNullOrWhiteSpace(validationBaseUrl)
                            || !Uri.TryCreate(validationBaseUrl, UriKind.Absolute, out var baseUri))
                        {
                            return;
                        }

                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                        var validationUri = new Uri(baseUri, "/api/auth/validate");
                        var postResponse = await httpClient.PostAsync(validationUri, null);

                        if (!postResponse.IsSuccessStatusCode)
                        {
                            context.Fail("Token validation failed");
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
