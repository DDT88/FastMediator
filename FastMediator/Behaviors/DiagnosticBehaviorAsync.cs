using FastMediator.Configuration;
using FastMediator.Interfaces;
using FastMediator.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che aggiunge diagnostica alle richieste asincrone
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta</typeparam>
    public class DiagnosticBehaviorAsync<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiagnosticBehaviorAsync<TRequest, TResponse>> _logger;
        private readonly FastMediatorOptions _options;

        /// <summary>
        /// Inizializza una nuova istanza del behavior di diagnostica asincrono
        /// </summary>
        public DiagnosticBehaviorAsync(IServiceProvider serviceProvider, FastMediatorOptions options)
        {
            _options = options;
            _logger = MediatorLoggerFactory.CreateLogger<DiagnosticBehaviorAsync<TRequest, TResponse>>(options);
            _serviceProvider = serviceProvider;
        }

        public int Order => 999; // Priorità molto bassa, eseguito per ultimo

        public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
        {
            var behaviors = _serviceProvider.GetServices(typeof(IPipelineBehaviorAsync<TRequest, TResponse>))
                .Cast<IPipelineBehaviorAsync<TRequest, TResponse>>()
                .ToList();

            _logger.LogDebug($"----- PIPELINE ASINCRONA PER {typeof(TRequest).Name} -----");
            _logger.LogDebug($"Behaviors asincroni registrati: {behaviors.Count}");

            foreach (var behavior in behaviors)
            {
                if (behavior == this) continue; // Salta questo behavior stesso

                var orderInfo = behavior is IOrderedPipelineBehavior ordered
                    ? $" (Ordine: {ordered.Order})"
                    : "";

                _logger.LogDebug($"- {behavior.GetType().Name}{orderInfo}");
            }

            _logger.LogDebug("----- FINE DIAGNOSTICA ASINCRONA -----");

            return await next(request, cancellationToken);
        }
    }
}
