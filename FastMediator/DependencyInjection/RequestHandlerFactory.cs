using System;
using System.Linq;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    /// <summary>
    /// Classe helper tipizzata per ogni combinazione di tipi di richiesta/risposta.
    /// Compila una sola volta (e mette in cache) il delegato che risolve handler e
    /// behaviors dal <see cref="IServiceProvider"/> ambient (lo scope corrente) e
    /// costruisce la pipeline.
    /// </summary>
    public class RequestHandlerFactory<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        // Comparazione per Order, memorizzata per tipo chiuso (nessuna allocazione per chiamata).
        private static readonly Comparison<IPipelineBehavior<TRequest, TResponse>> OrderComparison =
            (a, b) => ((a as IOrderedPipelineBehavior)?.Order ?? int.MaxValue)
                .CompareTo((b as IOrderedPipelineBehavior)?.Order ?? int.MaxValue);

        /// <summary>
        /// Crea (o recupera dalla cache) il delegato per l'handler.
        /// </summary>
        public static Func<IServiceProvider, object, object> CreateHandler()
        {
            return DelegateCache.Instance.GetOrCreateRequestHandler<TRequest, TResponse>(() =>
            {
                return (provider, request) =>
                {
                    // Risolviamo handler e behaviors dallo scope corrente (ambient),
                    // senza creare un nuovo scope per ogni richiesta.
                    var handler = provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

                    var behaviorsEnum = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
                    var behaviors = behaviorsEnum as IPipelineBehavior<TRequest, TResponse>[]
                                    ?? behaviorsEnum.ToArray();

                    // Fast-path: nessun behavior registrato -> invocazione diretta.
                    if (behaviors.Length == 0)
                    {
                        return handler.Handle((TRequest)request);
                    }

                    // Ordiniamo per Order solo se necessario (in-place, senza LINQ per chiamata).
                    if (behaviors.Length > 1)
                    {
                        Array.Sort(behaviors, OrderComparison);
                    }

                    // Costruiamo la pipeline in ordine inverso così che a runtime venga
                    // eseguita per Order crescente.
                    Func<TRequest, TResponse> pipeline = req => handler.Handle(req);
                    for (int i = behaviors.Length - 1; i >= 0; i--)
                    {
                        var behavior = behaviors[i];
                        var next = pipeline;
                        pipeline = req => behavior.Handle(req, next);
                    }

                    return pipeline((TRequest)request);
                };
            });
        }
    }
}
