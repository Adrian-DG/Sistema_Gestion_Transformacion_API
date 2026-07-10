using Application.Contracts;
using Domain.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infraestructure.Interceptors
{
    public class AuditableInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var context = eventData.Context;

            if (context is null) return base.SavingChanges(eventData, result);

            var userId = currentUserService.UserId;

            if (userId.HasValue)
            {
                foreach (var entry in context.ChangeTracker.Entries())
                {
                    if (entry.Entity is IAuditableEntityMetadata auditableEntity)
                    {
                        switch (entry.State)
                        {
                            case EntityState.Added:
                                auditableEntity.CreatedBy = userId.Value;
                                auditableEntity.CreatedAt = DateTime.UtcNow;
                                break;
                            case EntityState.Modified:
                                auditableEntity.UpdatedBy = userId.Value;
                                auditableEntity.UpdatedAt = DateTime.UtcNow;
                                break;
                        }
                    }
                }
            }

            return base.SavingChanges(eventData, result);
        }
    }
}
