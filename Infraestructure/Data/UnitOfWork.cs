using Application.Contracts;
using Application.Contracts.Recursos;
using Infraestructure.Repositories.Recursos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure.Data
{
    public class UnitOfWork(MainContext context) : IUnitOfWork
    {
        public IVehiculoRepository VehiculoRepository => new VehiculoRepository(context);
    }
}
        