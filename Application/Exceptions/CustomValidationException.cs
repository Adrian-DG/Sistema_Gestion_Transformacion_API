using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Exceptions
{
    public class CustomValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }
        public CustomValidationException(): base("Han ocurrido errores de validación.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public CustomValidationException(IEnumerable<ValidationFailure> failures) : this()
        {
            Errors = failures
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(
                    failureGroup => failureGroup.Key,
                    failureGroup => failureGroup.ToArray()
                );
        }
    }
}
