using FastMediator.Configuration;
using FastMediator.Interfaces;
using FastMediator.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che aggiunge diagnostica alle richieste
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta</typeparam>
    public class DiagnosticBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiagnosticBehavior<TRequest, TResponse>> _logger;
        private readonly FastMediatorOptions _options;


        /// <summary>
        /// Inizializza una nuova istanza del behavior di diagnostica
        /// </summary>
        public DiagnosticBehavior(IServiceProvider serviceProvider , FastMediatorOptions options)
        {
            _options = options;
            _logger = MediatorLoggerFactory.CreateLogger<DiagnosticBehavior<TRequest, TResponse>>(options);
            _serviceProvider = serviceProvider;
        }
   

        public int Order => 999; // Priorità molto bassa, eseguito per ultimo

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            var behaviors = _serviceProvider.GetServices(typeof(IPipelineBehavior<TRequest, TResponse>))
                .Cast<IPipelineBehavior<TRequest, TResponse>>()
                .ToList();

            _logger.LogDebug($"----- PIPELINE PER {typeof(TRequest).Name} -----");
            _logger.LogDebug($"Behaviors registrati: {behaviors.Count}");

            foreach (var behavior in behaviors)
            {
                if (behavior == this) continue; // Salta questo behavior stesso

                var orderInfo = behavior is IOrderedPipelineBehavior ordered
                    ? $" (Ordine: {ordered.Order})"
                    : "";

                _logger.LogDebug($"- {behavior.GetType().Name}{orderInfo}");
            }

            _logger.LogDebug("----- FINE DIAGNOSTICA -----");

            return next(request);
        }
    }
}
