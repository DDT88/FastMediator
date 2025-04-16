using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    // Classe helper tipizzata per ogni combinazione di tipi di richiesta/risposta asincrona
    public class AsyncRequestHandlerFactory<TRequest, TResponse>
        where TRequest : IAsyncRequest<TResponse>
    {
        // Metodo statico che crea il delegato per l'handler asincrono
        public static Func<IServiceProvider, object, CancellationToken, Task<object>> CreateHandler()
        {
            // Utilizziamo DelegateCache per memorizzare il delegato
            return DelegateCache.Instance.GetOrCreateAsyncRequestHandler<TRequest, TResponse>(() =>
            {
                return async (provider, request, cancellationToken) =>
                {
                    // Ottieni il factory per creare un nuovo scope
                    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                    // Crea un nuovo scope
                    using (var scope = scopeFactory.CreateScope())
                    {
                        // Usa il provider dello scope per risolvere l'handler
                        var handler = scope.ServiceProvider.GetRequiredService<IAsyncRequestHandler<TRequest, TResponse>>();

                        // E i behaviors
                        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehaviorAsync<TRequest, TResponse>>();

                        // Crea la funzione di base che invoca l'handler
                        Func<TRequest, CancellationToken, Task<TResponse>> pipeline = (req, token) =>
                            handler.HandleAsync(req, token);

                        var orderedBehaviors = behaviors
                                 .Select(b => new
                                 {
                                     Behavior = b,
                                     Order = (b as IOrderedPipelineBehavior)?.Order ?? int.MaxValue
                                 })
                                 .OrderBy(x => x.Order)
                                 .Select(x => x.Behavior);

                        // Costruisci la pipeline in ordine inverso
                        foreach (var behavior in orderedBehaviors)
                        {
                            var currentPipeline = pipeline;
                            pipeline = (req, token) => behavior.HandleAsync(req, currentPipeline, token);
                        }

                        // Esegui la pipeline
                        return await pipeline((TRequest)request, cancellationToken);
                    }
                };
            });
        }
    }
}