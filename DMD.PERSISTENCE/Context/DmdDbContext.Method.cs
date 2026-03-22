using DMD.DOMAIN.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace DMD.PERSISTENCE.Context
{
    public partial class DmdDbContext
    {
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user?.FindFirst(ClaimTypes.Email)?.Value;

            const string systemActorId = "system";
            const string systemActorName = "system";

            // Handle newly added entities
            foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
            {
                if (entry.Entity is BaseEntity<int> intEntity)
                {
                    intEntity.CreatedById = !string.IsNullOrEmpty(intEntity.CreatedById)
                        ? intEntity.CreatedById
                        : userId ?? systemActorId;
                    intEntity.CreatedBy = !string.IsNullOrEmpty(intEntity.CreatedBy)
                        ? intEntity.CreatedBy
                        : userEmail ?? systemActorName;
                }
                else if (entry.Entity is BaseEntity<long> longEntity)
                {
                    longEntity.CreatedById = !string.IsNullOrEmpty(longEntity.CreatedById)
                        ? longEntity.CreatedById
                        : userId ?? systemActorId;
                    longEntity.CreatedBy = !string.IsNullOrEmpty(longEntity.CreatedBy)
                        ? longEntity.CreatedBy
                        : userEmail ?? systemActorName;
                }
            }

            // Handle modified entities
            foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Modified))
            {
                if (entry.Entity is BaseEntity<int> intEntity)
                {
                    intEntity.LastUpdatedAt = DateTime.UtcNow;
                    intEntity.LastUpdatedById = !string.IsNullOrEmpty(userId) ? userId : systemActorId;
                    intEntity.LastUpdatedBy = !string.IsNullOrEmpty(userEmail) ? userEmail : systemActorName;

                }
                else if (entry.Entity is BaseEntity<long> longEntity)
                {
                    longEntity.LastUpdatedAt = DateTime.UtcNow;
                    longEntity.LastUpdatedById = !string.IsNullOrEmpty(userId) ? userId : systemActorId;
                    longEntity.LastUpdatedBy = !string.IsNullOrEmpty(userEmail) ? userEmail : systemActorName;
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
