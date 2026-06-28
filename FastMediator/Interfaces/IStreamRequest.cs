using System;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Rappresenta una richiesta che produrrà un flusso asincrono di risposte di tipo TResponse
    /// </summary>
    /// <typeparam name="TResponse">Il tipo di risposta prodotta nello stream</typeparam>
    public interface IStreamRequest<TResponse> { }
}
