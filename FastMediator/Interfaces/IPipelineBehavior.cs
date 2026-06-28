using System;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Definisce un comportamento della pipeline per l'elaborazione delle richieste
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta gestita</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta prodotta</typeparam>
    public interface IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Gestisce la richiesta e chiama il successivo gestore nella pipeline
        /// </summary>
        /// <param name="request">La richiesta da gestire</param>
        /// <param name="next">Il delegato che rappresenta il successivo elemento della pipeline</param>
        /// <returns>La risposta prodotta</returns>
        TResponse Handle(TRequest request, Func<TRequest, TResponse> next);
    }
}
