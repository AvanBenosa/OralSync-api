using DMD.DOMAIN.Entities.UserProfile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DMD.PERSISTENCE.Context
{
    public partial class DmdDbContext : IdentityDbContext<UserProfile, IdentityRole, string>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DmdDbContext(
            DbContextOptions<DmdDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null
        ) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
