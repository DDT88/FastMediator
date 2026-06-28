using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    /// <summary>
    /// Classe helper tipizzata per ogni combinazione di tipi di richiesta/risposta asincrona.
    /// Risolve handler e behaviors dallo scope corrente (ambient) senza creare un nuovo scope.
    /// </summary>
    public class AsyncRequestHandlerFactory<TRequest, TResponse>
        where TRequest : IAsyncRequest<TResponse>
    {
        // Comparazione per Order, memorizzata per tipo chiuso (nessuna allocazione per chiamata).
        private static readonly Comparison<IPipelineBehaviorAsync<TRequest, TResponse>> OrderComparison =
            (a, b) => ((a as IOrderedPipelineBehavior)?.Order ?? int.MaxValue)
                .CompareTo((b as IOrderedPipelineBehavior)?.Order ?? int.MaxValue);

        /// <summary>
        /// Crea (o recupera dalla cache) il delegato per l'handler asincrono.
        /// </summary>
        public static Func<IServiceProvider, object, CancellationToken, Task<object>> CreateHandler()
        {
            return DelegateCache.Instance.GetOrCreateAsyncRequestHandler<TRequest, TResponse>(() =>
            {
                return async (provider, request, cancellationToken) =>
                {
                    var handler = provider.GetRequiredService<IAsyncRequestHandler<TRequest, TResponse>>();

                    var behaviorsEnum = provider.GetServices<IPipelineBehaviorAsync<TRequest, TResponse>>();
                    var behaviors = behaviorsEnum as IPipelineBehaviorAsync<TRequest, TResponse>[]
                                    ?? behaviorsEnum.ToArray();

                    // Fast-path: nessun behavior registrato -> invocazione diretta.
                    if (behaviors.Length == 0)
                    {
                        return await handler.HandleAsync((TRequest)request, cancellationToken);
                    }

                    if (behaviors.Length > 1)
                    {
                        Array.Sort(behaviors, OrderComparison);
                    }

                    Func<TRequest, CancellationToken, Task<TResponse>> pipeline =
                        (req, token) => handler.HandleAsync(req, token);

                    for (int i = behaviors.Length - 1; i >= 0; i--)
                    {
                        var behavior = behaviors[i];
                        var next = pipeline;
                        pipeline = (req, token) => behavior.HandleAsync(req, next, token);
                    }

                    return await pipeline((TRequest)request, cancellationToken);
                };
            });
        }
    }
}
