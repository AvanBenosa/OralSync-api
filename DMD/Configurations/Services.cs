using DMD.SERVICES;
using DMD.SERVICES.Email;
using DMD.SERVICES.ProtectionProvider;

namespace DMD.API.Configurations
{
    public static class Services
    {
        internal static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDataProtection();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<IProtectionProvider, ProtectionProvider>();
            builder.Services.AddDmdEmailServices(builder.Configuration);
        }
    }
}
