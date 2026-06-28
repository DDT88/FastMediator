using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    // Classe helper tipizzata per ogni combinazione di tipi di stream request/response
    public class StreamRequestHandlerFactory<TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        // Metodo statico che crea il delegato per l'handler stream
        public static Func<IServiceProvider, object, CancellationToken, object> CreateHandler()
        {
            // Utilizziamo DelegateCache per memorizzare il delegato
            return DelegateCache.Instance.GetOrCreateStreamRequestHandler<TRequest, TResponse>(() =>
            {
                return (provider, request, cancellationToken) =>
                {
                    return HandleStream(provider, (TRequest)request, cancellationToken);
                };
            });
        }

        private static async IAsyncEnumerable<TResponse> HandleStream(IServiceProvider provider, TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Ottieni il factory per creare un nuovo scope che durerà per tutto il ciclo di vita dell'enumeratore
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            // Crea un nuovo scope per l'esecuzione dello stream
            using (var scope = scopeFactory.CreateScope())
            {
                // Usa il provider dello scope per risolvere l'handler
                var handler = scope.ServiceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();

                // E i behaviors dello stream
                var behaviors = scope.ServiceProvider.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>();

                // Crea la funzione di base che invoca l'handler
                Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> pipeline = (req, token) =>
                    handler.HandleStream(req, token);

                var orderedBehaviors = behaviors
                         .Select(b => new
                         {
                             Behavior = b,
                             Order = (b as IOrderedPipelineBehavior)?.Order ?? int.MaxValue
                         })
                         .OrderByDescending(x => x.Order)
                         .Select(x => x.Behavior);

                // Costruisci la pipeline in ordine inverso
                foreach (var behavior in orderedBehaviors)
                {
                    var currentPipeline = pipeline;
                    pipeline = (req, token) => behavior.HandleStream(req, currentPipeline, token);
                }

                // Esegui la pipeline e yielda i risultati, preservando il ciclo di vita dello scope
                var enumerable = pipeline(request, cancellationToken);
                await foreach (var item in enumerable.WithCancellation(cancellationToken))
                {
                    yield return item;
                }
            }
        }
    }
}
