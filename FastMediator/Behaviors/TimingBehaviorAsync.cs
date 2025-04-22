using FastMediator.Configuration;
using FastMediator.Interfaces;
using FastMediator.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che misura il tempo di esecuzione delle richieste asincrone
    /// </summary>
    public class TimingBehaviorAsync<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IAsyncRequest<TResponse>
    {
        private readonly ILogger<TimingBehaviorAsync<TRequest, TResponse>> _logger;
        private readonly FastMediatorOptions _options;
        public int Order => 998; // Priorità bassa, eseguito quasi per ultimo

        /// <summary>
        /// Inizializza una nuova istanza del behavior di timing asincrono
        /// </summary>
        public TimingBehaviorAsync(FastMediatorOptions options)
        {
            _options = options;
            _logger = MediatorLoggerFactory.CreateLogger<TimingBehaviorAsync<TRequest, TResponse>>(options);
        }

        public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug($"[Timing] Iniziando elaborazione asincrona {typeof(TRequest).Name}");

            var response = await next(request, cancellationToken);

            stopwatch.Stop();
            _logger.LogDebug($"[Timing] Completata elaborazione asincrona {typeof(TRequest).Name} in {stopwatch.ElapsedMilliseconds}ms");

            return response;
        }
    }
}