using FastMediator.Core;
using FastMediator.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Core
{
    /// <summary>
    /// Estensioni asincrone per il dispatcher
    /// </summary>
    public static class DispatcherAsyncExtensions
    {
        /// <summary>
        /// Invia una richiesta asincrona fortemente tipizzata
        /// </summary>
        public static Task<TResponse> SendAsync<TRequest, TResponse>(this Dispatcher dispatcher, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IAsyncRequest<TResponse>
        {
            return dispatcher.SendAsync<TResponse>(request, cancellationToken);
        }

        /// <summary>
        /// Pubblica una notifica asincrona fortemente tipizzata
        /// </summary>
        public static Task PublishAsync<TNotification>(this Dispatcher dispatcher, TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IAsyncNotification
        {
            return dispatcher.PublishAsync(notification, cancellationToken);
        }

        /// <summary>
        /// Pubblica una notifica asincrona in modo sequenziale
        /// </summary>
        public static Task PublishSequentialAsync<TNotification>(this Dispatcher dispatcher, TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IAsyncNotification
        {
            return dispatcher.PublishAsyncSequential(notification, cancellationToken);
        }

        /// <summary>
        /// Crea uno stream da una richiesta asincrona
        /// </summary>
        public static System.Collections.Generic.IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(this Dispatcher dispatcher, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IStreamRequest<TResponse>
        {
            return dispatcher.CreateStream<TResponse>(request, cancellationToken);
        }
    }
}