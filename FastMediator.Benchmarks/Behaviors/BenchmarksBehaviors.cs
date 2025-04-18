using FastMediator.Benchmarks.Models;
using FastMediator.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Benchmarks.Behaviors
{
    // Behaviors ottimizzati per benchmark

    // Behavior semplice senza operazioni costose
    public class LightweightBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        public int Order => 0;

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            // Comportamento molto leggero
            return next(request);
        }
    }

    // Behavior con un minimo di elaborazione
    public class StandardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        public int Order => 10;

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            // Aggiungi un po' di elaborazione
            var requestType = typeof(TRequest).Name;

            var response = next(request);

            return response;
        }
    }

    // Behavior con elaborazione più pesante
    public class HeavyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        public int Order => 20;

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            // Simulazione di un'operazione costosa (es. logging o elaborazione)
            var requestProperties = request.GetType().GetProperties();
            foreach (var prop in requestProperties)
            {
                // Semplicemente legge tutte le proprietà (simulando ispezione)
                var value = prop.GetValue(request);
            }

            var response = next(request);

            var responseType = response?.GetType();

            return response;
        }
    }

    // Behavior specifico per i benchmark
    public class BenchmarkMonitoringBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        public int Order => -100; // Eseguito per primo

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            var requestName = typeof(TRequest).Name;

            // Misura il tempo
            var startTime = DateTime.UtcNow;
            var response = next(request);
            var endTime = DateTime.UtcNow;
            var elapsed = (endTime - startTime).TotalMilliseconds;

            // Nel benchmark reale questi valori sarebbero registrati
            // ma qui evitiamo I/O per non influenzare le misurazioni

            return response;
        }
    }

    // Versioni asincrone degli stessi behavior

    public class AsyncLightweightBehavior<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IAsyncRequest<TResponse>
    {
        public int Order => 0;

        public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
        {
            return await next(request, cancellationToken);
        }
    }

    public class AsyncStandardBehavior<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IAsyncRequest<TResponse>
    {
        public int Order => 10;

        public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
        {
            var requestType = typeof(TRequest).Name;

            var response = await next(request, cancellationToken);

            return response;
        }
    }

    public class AsyncHeavyBehavior<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IAsyncRequest<TResponse>
    {
        public int Order => 20;

        public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
        {
            var requestProperties = request.GetType().GetProperties();
            foreach (var prop in requestProperties)
            {
                var value = prop.GetValue(request);
            }

            var response = await next(request, cancellationToken);

            var responseType = response?.GetType();

            return response;
        }
    }
}