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
            ApplyAuditMetadata(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ApplyAuditMetadata(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAuditMetadata(DbContext? context)
        {
            if (context is null) return;

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
        }
    }
}
