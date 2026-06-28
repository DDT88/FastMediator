using System;
using System.Collections.Generic;
using System.Threading;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Gestisce una richiesta asincrona sotto forma di stream (IAsyncEnumerable)
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta da gestire</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta da produrre nello stream</typeparam>
    public interface IStreamRequestHandler<TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        /// <summary>
        /// Gestisce la richiesta restituendo uno stream asincrono di risposte
        /// </summary>
        /// <param name="request">La richiesta da gestire</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        /// <returns>Un IAsyncEnumerable di risposte</returns>
        IAsyncEnumerable<TResponse> HandleStream(TRequest request, CancellationToken cancellationToken = default);
    }
}
