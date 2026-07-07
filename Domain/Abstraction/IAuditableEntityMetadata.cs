using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Abstraction
{
    public interface IAuditableEntityMetadata
    {
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
