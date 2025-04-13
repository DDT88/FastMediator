using System;
using System.Collections.Generic;
using FastMediator.Caching;
using FastMediator.Interfaces;

namespace FastMediator.Core
{
    /// <summary>
    /// Dispatching centrale per richieste e notifiche
    /// </summary>
    public class Dispatcher
    {
        private readonly IServiceProvider _provider;
        private readonly Dictionary<Type, Func<IServiceProvider, object, object>> _handlers;
        private readonly Dictionary<Type, List<Action<IServiceProvider, object>>> _notificationHandlers;

        /// <summary>
        /// Crea una nuova istanza del dispatcher
        /// </summary>
        public Dispatcher(IServiceProvider provider,
                          Dictionary<Type, Func<IServiceProvider, object, object>> handlers,
                          Dictionary<Type, List<Action<IServiceProvider, object>>> notificationHandlers)
        {
            _provider = provider;
            _handlers = handlers;
            _notificationHandlers = notificationHandlers;
        }

        /// <summary>
        /// Invia una richiesta e ottiene una risposta
        /// </summary>
        /// <typeparam name="TResponse">Il tipo di risposta atteso</typeparam>
        /// <param name="request">La richiesta da inviare</param>
        /// <returns>La risposta prodotta</returns>
        public TResponse Send<TResponse>(IRequest<TResponse> request)
        {
            var type = request.GetType();
            if (!_handlers.TryGetValue(type, out var handler))
                throw new InvalidOperationException($"Handler not found for request type {type.Name}");

            return (TResponse)handler(_provider, request);
        }

        /// <summary>
        /// Pubblica una notifica a tutti gli handler registrati
        /// </summary>
        /// <typeparam name="TNotification">Il tipo di notifica</typeparam>
        /// <param name="notification">La notifica da pubblicare</param>
        public void Publish<TNotification>(TNotification notification) where TNotification : INotification
        {
            var type = typeof(TNotification);
            if (_notificationHandlers.TryGetValue(type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler(_provider, notification);
                }
            }
        }

        /// <summary>
        /// Ottiene le statistiche di utilizzo della cache degli handler delle richieste
        /// </summary>
        public (int Hits, int Misses) GetRequestHandlerCacheStats() => DelegateCache.Instance.RequestStats;

        /// <summary>
        /// Ottiene le statistiche di utilizzo della cache degli handler delle notifiche
        /// </summary>
        public (int Hits, int Misses) GetNotificationHandlerCacheStats() => DelegateCache.Instance.NotificationStats;

        /// <summary>
        /// Ottiene la dimensione della cache degli handler delle richieste
        /// </summary>
        public int RequestHandlerCacheSize => DelegateCache.Instance.RequestHandlerCacheSize;

        /// <summary>
        /// Ottiene la dimensione della cache degli handler delle notifiche
        /// </summary>
        public int NotificationHandlerCacheSize => DelegateCache.Instance.NotificationHandlerCacheSize;
    }
}