using Application.Contracts.Recursos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts
{
    public interface IUnitOfWork
    {
        IVehiculoRepository VehiculoRepository { get; }
    }
}
