using System;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Gestisce una richiesta di tipo TRequest e produce una risposta di tipo TResponse
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta da gestire</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta da produrre</typeparam>
    public interface IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Gestisce la richiesta specificata
        /// </summary>
        /// <param name="request">La richiesta da gestire</param>
        /// <returns>La risposta prodotta</returns>
        TResponse Handle(TRequest request);
    }
}