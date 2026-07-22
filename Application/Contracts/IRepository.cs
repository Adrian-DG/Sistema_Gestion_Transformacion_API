using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Context { get; }
    }
}
