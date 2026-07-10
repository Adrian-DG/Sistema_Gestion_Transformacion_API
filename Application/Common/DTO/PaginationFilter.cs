using Application.Common.Response;
using Domain.Common;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.DTO;

public record PaginationFilterQuery<T> : IRequest<Result<PagedData<T>>> where T : class
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string Param { get; set; }
    public PaginationFilterQuery(int pageNumber, int pageSize, string param)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        Param = param;
    }
}

public class PaginationFilterQueryValidator<T> : AbstractValidator<PaginationFilterQuery<T>> where T : class
{
    public PaginationFilterQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("El número de página debe ser mayor que 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("El tamaño de página debe ser mayor que 0.");
    }
}