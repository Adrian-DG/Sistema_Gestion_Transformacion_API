using Domain.Common;
using FluentValidation;
using MediatR;

namespace Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
        where TResponse : Result
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var errors = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
                .ToArray();

            if (errors.Length == 0)
            {
                return await next();
            }

            return CreateValidationResult<TResponse>(errors);
        }

        private static TResult CreateValidationResult<TResult>(Error[] errors) where TResult : Result
        {
            var validationError = ValidationError.FromErrors(errors);

            if (typeof(TResult) == typeof(Result))
            {
                return (TResult)(object)Result.Failure(validationError);
            }

            var valueType = typeof(TResult).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                .MakeGenericMethod(valueType);

            return (TResult)failureMethod.Invoke(null, [validationError])!;
        }
    }
}
