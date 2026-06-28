using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Caching;
using FastMediator.Interfaces;

namespace FastMediator.Core
{
    /// <summary>
    /// Punto di ingresso del mediator. Risolve handler e behaviors dallo scope corrente
    /// (ambient): è registrato come scoped, quindi in una richiesta web le dipendenze
    /// scoped degli handler condividono lo scope della richiesta. I delegati compilati
    /// sono condivisi tramite <see cref="DispatcherRegistry"/> (singleton).
    /// </summary>
    public sealed class Dispatcher
    {
        private readonly IServiceProvider _provider;
        private readonly DispatcherRegistry _registry;

        /// <summary>
        /// Inizializza una nuova istanza del dispatcher.
        /// </summary>
        public Dispatcher(IServiceProvider provider, DispatcherRegistry registry)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Invia una richiesta e restituisce la risposta prodotta dal relativo handler.
        /// </summary>
        public TResponse Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var handler = _registry.GetRequestHandler(request.GetType(), typeof(TResponse));
            return (TResponse)handler(_provider, request);
        }

        /// <summary>
        /// Invia una richiesta asincrona e restituisce la risposta prodotta dal relativo handler.
        /// </summary>
        public async Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var handler = _registry.GetAsyncRequestHandler(request.GetType(), typeof(TResponse));
            return (TResponse)await handler(_provider, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Pubblica una notifica a tutti gli handler registrati. La selezione degli handler
        /// usa il tipo a runtime della notifica (supporto polimorfico).
        /// </summary>
        public void Publish<TNotification>(TNotification notification) where TNotification : INotification
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            var handlers = _registry.GetNotificationHandlers(notification.GetType());
            if (handlers is null) return;

            for (int i = 0; i < handlers.Count; i++)
            {
                handlers[i](_provider, notification);
            }
        }

        /// <summary>
        /// Pubblica una notifica asincrona a tutti gli handler registrati eseguendoli in parallelo.
        /// </summary>
        public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IAsyncNotification
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            var handlers = _registry.GetAsyncNotificationHandlers(notification.GetType());
            if (handlers is null || handlers.Count == 0) return;

            var tasks = new Task[handlers.Count];
            for (int i = 0; i < handlers.Count; i++)
            {
                tasks[i] = handlers[i](_provider, notification, cancellationToken);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Pubblica una notifica asincrona eseguendo gli handler in modo sequenziale.
        /// </summary>
        public async Task PublishAsyncSequential<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IAsyncNotification
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            var handlers = _registry.GetAsyncNotificationHandlers(notification.GetType());
            if (handlers is null) return;

            for (int i = 0; i < handlers.Count; i++)
            {
                await handlers[i](_provider, notification, cancellationToken).ConfigureAwait(false);
            }
        }

        #region Statistiche cache (diagnostica)

        /// <summary>Statistiche (hit/miss) della cache degli handler delle richieste.</summary>
        public (int Hits, int Misses) GetRequestHandlerCacheStats() => DelegateCache.Instance.RequestStats;

        /// <summary>Statistiche (hit/miss) della cache degli handler delle notifiche.</summary>
        public (int Hits, int Misses) GetNotificationHandlerCacheStats() => DelegateCache.Instance.NotificationStats;

        /// <summary>Statistiche (hit/miss) della cache degli handler asincroni delle richieste.</summary>
        public (int Hits, int Misses) GetAsyncRequestHandlerCacheStats() => DelegateCache.Instance.AsyncRequestStats;

        /// <summary>Statistiche (hit/miss) della cache degli handler asincroni delle notifiche.</summary>
        public (int Hits, int Misses) GetAsyncNotificationHandlerCacheStats() => DelegateCache.Instance.AsyncNotificationStats;

        /// <summary>Dimensione della cache degli handler delle richieste.</summary>
        public int RequestHandlerCacheSize => DelegateCache.Instance.RequestHandlerCacheSize;

        /// <summary>Dimensione della cache degli handler delle notifiche.</summary>
        public int NotificationHandlerCacheSize => DelegateCache.Instance.NotificationHandlerCacheSize;

        /// <summary>Dimensione della cache degli handler asincroni delle richieste.</summary>
        public int AsyncRequestHandlerCacheSize => DelegateCache.Instance.AsyncRequestHandlerCacheSize;

        /// <summary>Dimensione della cache degli handler asincroni delle notifiche.</summary>
        public int AsyncNotificationHandlerCacheSize => DelegateCache.Instance.AsyncNotificationHandlerCacheSize;

        #endregion
    }
}
