using System;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Rappresenta una richiesta che produrrà una risposta di tipo TResponse
    /// </summary>
    /// <typeparam name="TResponse">Il tipo di risposta prodotta</typeparam>
    public interface IRequest<TResponse> { }
}
