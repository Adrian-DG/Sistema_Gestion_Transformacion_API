using Application.Contracts;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected readonly IMediator _mediator;

        protected ApiControllerBase(IMediator mediator)
        {
            _mediator = mediator;
        }

        protected IActionResult HandleFailure(Result result)
        {
            if (result.IsSuccess)
            {
                throw new InvalidOperationException("HandleFailure no debe invocarse con un resultado exitoso.");
            }

            if (result.Error is ValidationError validationError)
            {
                var modelState = new ModelStateDictionary();

                foreach (var error in validationError.Errors)
                {
                    modelState.AddModelError(error.Code, error.Message);
                }

                return ValidationProblem(modelState);
            }

            var statusCode = result.Error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: statusCode);
        }
    }
}
