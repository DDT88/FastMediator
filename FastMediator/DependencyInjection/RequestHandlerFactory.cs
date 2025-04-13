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
            return (provider, request) =>
            {
                // Ottieni l'handler dal service provider
                var handler = provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

                // Ottieni tutti i behaviors
                var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

                // Crea la funzione di base che invoca l'handler
                Func<TRequest, TResponse> pipeline = req => handler.Handle(req);

                // Costruisci la pipeline in ordine inverso
                foreach (var behavior in ((System.Collections.Generic.IEnumerable<IPipelineBehavior<TRequest, TResponse>>)behaviors).Reverse())
                {
                    var currentPipeline = pipeline;
                    pipeline = req => behavior.Handle(req, currentPipeline);
                }

                // Esegui la pipeline
                return pipeline((TRequest)request);
            };
        }
    }
}