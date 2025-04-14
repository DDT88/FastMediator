using FastMediator.Interfaces;
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
        public int Order => 998; // Priorità bassa, eseguito quasi per ultimo

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"[Timing] Iniziando elaborazione {typeof(TRequest).Name}");

            var response = next(request);

            stopwatch.Stop();
            Console.WriteLine($"[Timing] Completata elaborazione {typeof(TRequest).Name} in {stopwatch.ElapsedMilliseconds}ms");

            return response;
        }
    }
}
