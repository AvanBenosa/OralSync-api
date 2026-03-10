
using DMD.SERVICES;

namespace DMD.API.Configurations
{
    public static class Services
    {
        internal static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<TokenService>();
        }
    }
}
