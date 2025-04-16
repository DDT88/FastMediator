using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Gestisce una richiesta asincrona di tipo TRequest e produce una risposta di tipo TResponse
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta da gestire</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta da produrre</typeparam>
    public interface IAsyncRequestHandler<TRequest, TResponse>
        where TRequest : IAsyncRequest<TResponse>
    {
        /// <summary>
        /// Gestisce la richiesta specificata in modo asincrono
        /// </summary>
        /// <param name="request">La richiesta da gestire</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        /// <returns>La risposta prodotta</returns>
        Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
