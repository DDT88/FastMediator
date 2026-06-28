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


        private readonly Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> _asyncHandlers = new();
        private readonly Dictionary<Type, List<Func<IServiceProvider, object, CancellationToken, Task>>> _asyncNotificationHandlers = new();
        private readonly ConcurrentDictionary<Type, object> _asyncHandlerFactories;

        private readonly Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>> _streamHandlers = new();
        private readonly ConcurrentDictionary<Type, object> _streamHandlerFactories;

        /// <summary>
        /// Crea una nuova istanza del dispatcher
        /// </summary>
        public Dispatcher(IServiceProvider provider,
                           Dictionary<Type, Func<IServiceProvider, object, object>> handlers,
                           Dictionary<Type, List<Action<IServiceProvider, object>>> notificationHandlers,
                           Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> asyncHandlers,
                           Dictionary<Type, List<Func<IServiceProvider, object, CancellationToken, Task>>> asyncNotificationHandlers,
                           Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>> streamHandlers,
                           FastMediatorOptions options) 
        {
            _asyncHandlers = asyncHandlers;
            _asyncNotificationHandlers = asyncNotificationHandlers;
            _streamHandlers = streamHandlers ?? new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>();

            _provider = provider;
            _handlers = handlers;
            _notificationHandlers = notificationHandlers;
            _options = options;

            // Se stiamo usando lazy loading o modalità ibrida, inizializza il dizionario delle factory
            if (options.RegistrationMode != HandlerRegistrationMode.Startup)
            {
                _handlerFactories = new ConcurrentDictionary<Type, object>();
                _asyncHandlerFactories = new ConcurrentDictionary<Type, object>();
                _streamHandlerFactories = new ConcurrentDictionary<Type, object>();
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
        /// Invia una richiesta asincrona e ottiene una risposta
        /// </summary>
        /// <typeparam name="TResponse">Il tipo di risposta atteso</typeparam>
        /// <param name="request">La richiesta da inviare</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        /// <returns>La risposta prodotta</returns>
        public async Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var type = request.GetType();

            // Approccio standard
            if (_options.RegistrationMode == HandlerRegistrationMode.Startup || _asyncHandlers.ContainsKey(type))
            {
                if (!_asyncHandlers.TryGetValue(type, out var handlerStd))
                    throw new InvalidOperationException($"Async handler not found for request type {type.Name}");

                return (TResponse)await handlerStd(_provider, request, cancellationToken);
            }

            // Approccio dinamico (lazy loading o ibrido per tipi non pre-registrati)
            var responseType = typeof(TResponse);
            var handler = GetOrCreateAsyncHandler(type, responseType);
            return (TResponse)await handler(_provider, request, cancellationToken);
        }

        /// <summary>
        /// Pubblica una notifica asincrona a tutti gli handler registrati
        /// </summary>
        /// <typeparam name="TNotification">Il tipo di notifica</typeparam>
        /// <param name="notification">La notifica da pubblicare</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IAsyncNotification
        {
            var type = typeof(TNotification);
            if (_asyncNotificationHandlers.TryGetValue(type, out var handlers))
            {
                var tasks = handlers.Select(handler => handler(_provider, notification, cancellationToken));
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Pubblica una notifica asincrona a tutti gli handler registrati in modo sequenziale
        /// </summary>
        public async Task PublishAsyncSequential<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IAsyncNotification
        {
            var type = typeof(TNotification);
            if (_asyncNotificationHandlers.TryGetValue(type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    await handler(_provider, notification, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Crea uno stream di risposte da una richiesta stream
        /// </summary>
        /// <typeparam name="TResponse">Il tipo di risposta atteso nello stream</typeparam>
        /// <param name="request">La richiesta stream da inviare</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        /// <returns>Uno stream IAsyncEnumerable di risposte</returns>
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var type = request.GetType();

            // Approccio standard
            if (_options.RegistrationMode == HandlerRegistrationMode.Startup || _streamHandlers.ContainsKey(type))
            {
                if (!_streamHandlers.TryGetValue(type, out var handlerStd))
                    throw new InvalidOperationException($"Stream handler not found for request type {type.Name}");

                return (IAsyncEnumerable<TResponse>)handlerStd(_provider, request, cancellationToken);
            }

            // Approccio dinamico (lazy loading o ibrido per tipi non pre-registrati)
            var responseType = typeof(TResponse);
            var handler = GetOrCreateStreamHandler(type, responseType);
            return (IAsyncEnumerable<TResponse>)handler(_provider, request, cancellationToken);
        }

        // Nuovo metodo per lazy loading degli stream handler
        private Func<IServiceProvider, object, CancellationToken, object> GetOrCreateStreamHandler(Type requestType, Type responseType)
        {
            return _streamHandlerFactories.GetOrAdd(requestType, _ => {
                // Crea il tipo della factory
                var factoryType = typeof(StreamRequestHandlerFactory<,>).MakeGenericType(requestType, responseType);

                // Chiama il metodo statico CreateHandler
                var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                var handlerDelegate = (Func<IServiceProvider, object, CancellationToken, object>)createMethod.Invoke(null, null);

                return handlerDelegate;
            }) as Func<IServiceProvider, object, CancellationToken, object>;
        }

        // Nuovo metodo per lazy loading degli handler asincroni
        private Func<IServiceProvider, object, CancellationToken, Task<object>> GetOrCreateAsyncHandler(Type requestType, Type responseType)
        {
            return _asyncHandlerFactories.GetOrAdd(requestType, _ => {
                // Crea il tipo della factory
                var factoryType = typeof(AsyncRequestHandlerFactory<,>).MakeGenericType(requestType, responseType);

                // Chiama il metodo statico CreateHandler
                var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                var handlerDelegate = (Func<IServiceProvider, object, CancellationToken, Task<object>>)createMethod.Invoke(null, null);

                // Registra il delegato anche nella mappa principale per future chiamate
                _asyncHandlers[requestType] = handlerDelegate;

                return handlerDelegate;
            }) as Func<IServiceProvider, object, CancellationToken, Task<object>>;
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


        /// <summary>
        /// Ottiene le statistiche di utilizzo della cache degli handler asincroni delle richieste
        /// </summary>
        public (int Hits, int Misses) GetAsyncRequestHandlerCacheStats() => DelegateCache.Instance.AsyncRequestStats;

        /// <summary>
        /// Ottiene le statistiche di utilizzo della cache degli handler asincroni delle notifiche
        /// </summary>
        public (int Hits, int Misses) GetAsyncNotificationHandlerCacheStats() => DelegateCache.Instance.AsyncNotificationStats;

        /// <summary>
        /// Ottiene la dimensione della cache degli handler asincroni delle richieste
        /// </summary>
        public int AsyncRequestHandlerCacheSize => DelegateCache.Instance.AsyncRequestHandlerCacheSize;

        /// <summary>
        /// Ottiene la dimensione della cache degli handler asincroni delle notifiche
        /// </summary>
        public int AsyncNotificationHandlerCacheSize => DelegateCache.Instance.AsyncNotificationHandlerCacheSize;

        /// <summary>
        /// Ottiene le statistiche di utilizzo della cache degli stream handler
        /// </summary>
        public (int Hits, int Misses) GetStreamRequestHandlerCacheStats() => DelegateCache.Instance.StreamRequestStats;

        /// <summary>
        /// Ottiene la dimensione della cache degli stream handler
        /// </summary>
        public int StreamRequestHandlerCacheSize => DelegateCache.Instance.StreamRequestHandlerCacheSize;

    }
}