using Domain.Abstraction;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure.Interceptors
{
    public class AuditableInterceptor : SaveChangesInterceptor
    {
        private readonly Guid? _userId;
        public AuditableInterceptor(Guid? userId)
        {
            _userId = userId;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var context = eventData.Context;

            if (context is null) return base.SavingChanges(eventData, result);
            
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is IAuditableEntityMetadata auditableEntity && _userId.HasValue)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditableEntity.CreatedBy = _userId.Value;
                            auditableEntity.CreatedAt = DateTime.UtcNow;
                            break;
                        case EntityState.Modified:
                            auditableEntity.UpdatedBy = _userId.Value;
                            auditableEntity.UpdatedAt = DateTime.UtcNow;
                            break;
                    }                    
                }
            }

            return base.SavingChanges(eventData, result);
        }
    }
}
