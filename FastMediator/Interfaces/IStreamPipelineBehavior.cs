using System;
using System.Collections.Generic;
using System.Threading;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Definisce un comportamento della pipeline per l'elaborazione di stream asincroni
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta gestita</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta prodotta nello stream</typeparam>
    public interface IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        /// <summary>
        /// Gestisce la richiesta stream e chiama il successivo gestore nella pipeline
        /// </summary>
        /// <param name="request">La richiesta da gestire</param>
        /// <param name="next">Il delegato che rappresenta il successivo elemento della pipeline, che restituisce l'IAsyncEnumerable originale</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        /// <returns>Il flusso IAsyncEnumerable risultante (potenzialmente modificato o intercettato)</returns>
        IAsyncEnumerable<TResponse> HandleStream(TRequest request, Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> next, CancellationToken cancellationToken = default);
    }
}
