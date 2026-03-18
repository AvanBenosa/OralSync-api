using DMD.SERVICES;
using DMD.SERVICES.Email;

namespace DMD.API.Configurations
{
    public static class Services
    {
        internal static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddDmdEmailServices(builder.Configuration);
        }
    }
}
