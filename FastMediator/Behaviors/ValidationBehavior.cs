using FastMediator.Configuration;
using FastMediator.Interfaces;
using FastMediator.Logging;
using FastMediator.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che esegue la validazione delle richieste
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta</typeparam>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;
        private readonly FastMediatorOptions _options;

        /// <summary>
        /// Inizializza una nuova istanza del behavior di validazione
        /// </summary>
        public ValidationBehavior(IServiceProvider serviceProvider, FastMediatorOptions options)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = MediatorLoggerFactory.CreateLogger<ValidationBehavior<TRequest, TResponse>>(options);
        }

        /// <summary>
        /// Ordine di esecuzione del behavior (più basso = priorità maggiore)
        /// </summary>
        public int Order => -10; // Esegui prima degli altri behavior

        /// <summary>
        /// Gestisce la richiesta eseguendo la validazione
        /// </summary>
        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            // Ottieni tutti i validatori per questo tipo di richiesta
            var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();

            if (validators.Any())
            {
                _logger.LogDebug("Trovati {ValidatorCount} validatori per la richiesta {RequestType}",
                    validators.Count, typeof(TRequest).Name);

                // Esegui tutti i validatori e raccogli gli errori
                var validationResults = new List<ValidationResult>();
                foreach (var validator in validators)
                {
                    var result = validator.Validate(request);
                    validationResults.Add(result);
                }

                // Verifica se ci sono errori di validazione
                var errors = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(e => e != null)
                    .ToList();

                if (errors.Any())
                {
                    _logger.LogWarning("Validazione fallita per la richiesta {RequestType}. Errori: {ErrorCount}",
                        typeof(TRequest).Name, errors.Count);

                    throw new ValidationException(errors);
                }
            }

            // La validazione è passata, procedi con la pipeline
            return next(request);
        }
    }
}