using FastMediator.Core;
using FastMediator.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Core
{
    /// <summary>
    /// Estensioni per l'interoperabilità tra chiamate sincrone e asincrone
    /// </summary>
    public static class SynchronizationExtensions
    {
        /// <summary>
        /// Invia una richiesta asincrona usando un handler sincrono (wrapper)
        /// </summary>
        /// <remarks>
        /// Questo metodo permette di inviare richieste sincrone utilizzando l'API asincrona.
        /// Può essere utile durante la migrazione da codice sincrono a asincrono.
        /// </remarks>
        public static Task<TResponse> SendAsAsync<TRequest, TResponse>(this Dispatcher dispatcher, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>
        {
            // Esegue l'operazione sincrona in un Task
            return Task.Run(() => dispatcher.Send(request), cancellationToken);
        }

        /// <summary>
        /// Pubblica una notifica sincrona usando l'API asincrona (wrapper)
        /// </summary>
        public static Task PublishAsAsync<TNotification>(this Dispatcher dispatcher, TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Task.Run(() => dispatcher.Publish(notification), cancellationToken);
        }

        /// <summary>
        /// Invia una richiesta asincrona in modo sincrono, bloccando fino al completamento
        /// </summary>
        /// <remarks>
        /// Questo metodo dovrebbe essere usato con cautela, preferibilmente solo in contesti
        /// che non supportano async/await (ad esempio, i costruttori).
        /// </remarks>
        public static TResponse SendSync<TRequest, TResponse>(this Dispatcher dispatcher, TRequest request)
            where TRequest : IAsyncRequest<TResponse>
        {
            // Blocca e attende il risultato
            return dispatcher.SendAsync<TResponse>(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Pubblica una notifica asincrona in modo sincrono, bloccando fino al completamento
        /// </summary>
        public static void PublishSync<TNotification>(this Dispatcher dispatcher, TNotification notification)
            where TNotification : IAsyncNotification
        {
            dispatcher.PublishAsync(notification).GetAwaiter().GetResult();
        }
    }
}