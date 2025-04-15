using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using FastMediator.Caching;
using FastMediator.Configuration;
using FastMediator.DependencyInjection;
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
        private readonly FastMediatorOptions _options;

        // Nuovi campi per l'approccio ibrido
        private readonly ConcurrentDictionary<Type, object> _handlerFactories;

        /// <summary>
        /// Crea una nuova istanza del dispatcher
        /// </summary>
        public Dispatcher(IServiceProvider provider,
                          Dictionary<Type, Func<IServiceProvider, object, object>> handlers,
                          Dictionary<Type, List<Action<IServiceProvider, object>>> notificationHandlers,
                          FastMediatorOptions options)
        {
            _provider = provider;
            _handlers = handlers;
            _notificationHandlers = notificationHandlers;
            _options = options;

            // Se stiamo usando lazy loading o modalità ibrida, inizializza il dizionario delle factory
            if (options.RegistrationMode != HandlerRegistrationMode.Startup)
            {
                _handlerFactories = new ConcurrentDictionary<Type, object>();
            }
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
            // Approccio standard
            if (_options.RegistrationMode == HandlerRegistrationMode.Startup || _handlers.ContainsKey(type))
            {
                if (!_handlers.TryGetValue(type, out var handlerStd))
                    throw new InvalidOperationException($"Handler not found for request type {type.Name}");

                return (TResponse)handlerStd(_provider, request);
            }

            // Approccio dinamico (lazy loading o ibrido per tipi non pre-registrati)
            var responseType = typeof(TResponse);
            var handler = GetOrCreateHandler(type, responseType);
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


        // Nuovo metodo per lazy loading degli handler
        private Func<IServiceProvider, object, object> GetOrCreateHandler(Type requestType, Type responseType)
        {
            return _handlerFactories.GetOrAdd(requestType, _ => {
                // Crea il tipo della factory
                var factoryType = typeof(RequestHandlerFactory<,>).MakeGenericType(requestType, responseType);

                // Chiama il metodo statico CreateHandler
                var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                var handlerDelegate = (Func<IServiceProvider, object, object>)createMethod.Invoke(null, null);

                // Registra il delegato anche nella mappa principale per future chiamate
                _handlers[requestType] = handlerDelegate;

                return handlerDelegate;
            }) as Func<IServiceProvider, object, object>;
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