using System;
using FastMediator.Configuration;
using Microsoft.Extensions.Logging;

namespace FastMediator.Logging
{
    /// <summary>
    /// Factory per la creazione di logger per i componenti di FastMediator
    /// </summary>
    public static class MediatorLoggerFactory
    {
        /// <summary>
        /// Crea un logger tipizzato per il componente specificato.
        /// Se non è disponibile un LoggerFactory nelle opzioni, restituisce un logger nullo.
        /// </summary>
        /// <typeparam name="T">Il tipo per cui creare il logger</typeparam>
        /// <param name="options">Le opzioni di configurazione del mediator</param>
        /// <returns>Un'istanza di ILogger<T></returns>
        public static ILogger<T> CreateLogger<T>(FastMediatorOptions options)
        {
            if (options?.LoggerFactory != null)
            {
                return options.LoggerFactory.CreateLogger<T>();
            }

            return new NullLogger<T>();
        }

        /// <summary>
        /// Logger nullo che implementa ILogger<T> ma non esegue alcuna operazione
        /// </summary>
        private class NullLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();
                public void Dispose() { }
            }
        }
    }
}