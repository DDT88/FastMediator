using System;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    // Classe helper tipizzata per ogni combinazione di tipi di richiesta/risposta
    public class RequestHandlerFactory<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        // Metodo statico che crea il delegato per l'handler
        public static Func<IServiceProvider, object, object> CreateHandler()
        {
            // Utilizziamo DelegateCache per memorizzare il delegato
            return DelegateCache.Instance.GetOrCreateRequestHandler<TRequest, TResponse>(() =>
            {
                return (provider, request) =>
                {
                    // Ottieni il factory per creare un nuovo scope
                    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                    // Crea un nuovo scope
                    using (var scope = scopeFactory.CreateScope())
                    {

                        // Usa il provider dello scope per risolvere l'handler
                        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

                        // E i behaviors
                        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

                        // Crea la funzione di base che invoca l'handler
                        Func<TRequest, TResponse> pipeline = req => handler.Handle(req);


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
                            pipeline = req => behavior.Handle(req, currentPipeline);
                        }

                        // Esegui la pipeline
                        return pipeline((TRequest)request);
                    }
                };
            });
        }
    }
}