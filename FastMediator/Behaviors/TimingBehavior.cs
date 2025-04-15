using FastMediator.Configuration;
using FastMediator.Interfaces;
using FastMediator.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che misura il tempo di esecuzione delle richieste
    /// </summary>
    public class TimingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TimingBehavior<TRequest, TResponse>> _logger;
        private readonly FastMediatorOptions _options;
        public int Order => 998; // Priorità bassa, eseguito quasi per ultimo


        /// <summary>
        /// Inizializza una nuova istanza del behavior di timing
        /// </summary>
        public TimingBehavior(FastMediatorOptions options)
        {
            _options = options;
            _logger = MediatorLoggerFactory.CreateLogger<TimingBehavior<TRequest, TResponse>>(options);
        }

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation($"[Timing] Iniziando elaborazione {typeof(TRequest).Name}");

            var response = next(request);

            stopwatch.Stop();
            _logger.LogInformation($"[Timing] Completata elaborazione {typeof(TRequest).Name} in {stopwatch.ElapsedMilliseconds}ms");

            return response;
        }
    }
}
