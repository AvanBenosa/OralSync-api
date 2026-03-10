using DMD.DOMAIN.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace DMD.PERSISTENCE.Context
{
    public partial class DmdDbContext
    {
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

            // Handle newly added entities
            foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
            {
                if (entry.Entity is BaseEntity<int> intEntity)
                {
                    intEntity.CreatedById = !string.IsNullOrEmpty(intEntity.CreatedById) ? intEntity.CreatedById : userId ?? null;
                    intEntity.CreatedBy = !string.IsNullOrEmpty(intEntity.CreatedBy) ? intEntity.CreatedBy : userEmail ?? null;
                }
                else if (entry.Entity is BaseEntity<long> longEntity)
                {
                    longEntity.CreatedById = !string.IsNullOrEmpty(longEntity.CreatedById) ? longEntity.CreatedById : userId ?? null;
                    longEntity.CreatedBy = !string.IsNullOrEmpty(longEntity.CreatedBy) ? longEntity.CreatedBy : userEmail ?? null;
                }
            }

            // Handle modified entities
            foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Modified))
            {
                if (entry.Entity is BaseEntity<int> intEntity)
                {
                    intEntity.LastUpdatedAt = DateTime.UtcNow;
                    intEntity.LastUpdatedById = !string.IsNullOrEmpty(userId) ? userId : "n/a";
                    intEntity.LastUpdatedBy = !string.IsNullOrEmpty(userEmail) ? userEmail : "n/a";

                }
                else if (entry.Entity is BaseEntity<long> longEntity)
                {
                    longEntity.LastUpdatedAt = DateTime.UtcNow;
                    longEntity.LastUpdatedById = !string.IsNullOrEmpty(userId) ? userId : "n/a";
                    longEntity.LastUpdatedBy = !string.IsNullOrEmpty(userEmail) ? userEmail : "n/a";
                }
            }

            return await base.SaveChangesAsync(true, cancellationToken);
        }

        static bool IsTypeof<T>(object t)
        {
            return t is T;
        }
    }
}
