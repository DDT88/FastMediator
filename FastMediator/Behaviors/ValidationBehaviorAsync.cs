using FastMediator.Configuration;
using FastMediator.Interfaces;
using FastMediator.Logging;
using FastMediator.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che esegue la validazione delle richieste in modo asincrono
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta</typeparam>
    public class ValidationBehaviorAsync<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ValidationBehaviorAsync<TRequest, TResponse>> _logger;
        private readonly FastMediatorOptions _options;

        /// <summary>
        /// Inizializza una nuova istanza del behavior di validazione asincrono
        /// </summary>
        public ValidationBehaviorAsync(IServiceProvider serviceProvider, FastMediatorOptions options)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = MediatorLoggerFactory.CreateLogger<ValidationBehaviorAsync<TRequest, TResponse>>(options);
        }

        /// <summary>
        /// Ordine di esecuzione del behavior (più basso = priorità maggiore)
        /// </summary>
        public int Order => -10; // Esegui prima degli altri behavior

        /// <summary>
        /// Gestisce la richiesta eseguendo la validazione in modo asincrono
        /// </summary>
        public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
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
            return await next(request, cancellationToken);
        }
    }
}
