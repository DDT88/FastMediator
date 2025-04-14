using FastMediator.Interfaces;
using FastMediator.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che esegue la validazione delle richieste
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public int Order => 1; // Alta priorità: la validazione dovrebbe essere eseguita presto

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            // Se non ci sono validatori per questo tipo di richiesta, procedi
            if (!_validators.Any())
            {
                return next(request);
            }

            // Esegui tutti i validatori e unisci i risultati
            var errors = _validators
                .Select(v => v.Validate(request))
                .SelectMany(result => result.Errors)
                .ToList();

            // Se ci sono errori, lancia un'eccezione
            if (errors.Any())
            {
                throw new ValidationException(errors);
            }

            // Nessun errore, procedi con la pipeline
            return next(request);
        }
    }

    /// <summary>
    /// Eccezione lanciata quando la validazione fallisce
    /// </summary>
    public class ValidationException : Exception
    {
        public IReadOnlyList<ValidationError> Errors { get; }

        public ValidationException(IReadOnlyList<ValidationError> errors)
            : base($"Validation failed: {errors.Count} error(s)")
        {
            Errors = errors;
        }
    }
}
