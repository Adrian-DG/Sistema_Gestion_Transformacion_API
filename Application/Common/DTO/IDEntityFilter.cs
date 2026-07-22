using Domain.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.DTO;

public record IDEntityFilterQuery<T>(Guid Id) : IRequest<Result<T>> where T : class;