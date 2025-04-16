using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Definisce un comportamento asincrono della pipeline per l'elaborazione delle richieste
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta gestita</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta prodotta</typeparam>
    public interface IPipelineBehaviorAsync<TRequest, TResponse>
        where TRequest : IAsyncRequest<TResponse>
    {
        /// <summary>
        /// Gestisce la richiesta in modo asincrono e chiama il successivo gestore nella pipeline
        /// </summary>
        /// <param name="request">La richiesta da gestire</param>
        /// <param name="next">Il delegato che rappresenta il successivo elemento della pipeline</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        /// <returns>La risposta prodotta</returns>
        Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default);
    }
}