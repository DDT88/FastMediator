using System;
using System.Collections.Concurrent;
using System.Threading;
using FastMediator.Interfaces;

namespace FastMediator.Caching
{
    /// <summary>
    /// Cache per i delegati compilati
    /// </summary>
    public class DelegateCache
    {
        private static readonly Lazy<DelegateCache> _instance = new Lazy<DelegateCache>(() => new DelegateCache());

        // Cache per i delegati degli handler delle richieste
        private readonly ConcurrentDictionary<RequestCacheKey, Func<IServiceProvider, object, object>> _requestHandlerCache = new();

        // Cache per i delegati degli handler delle notifiche
        private readonly ConcurrentDictionary<Type, Action<IServiceProvider, object>> _notificationHandlerCache = new();

        // Cache per i delegati degli handler asincroni delle richieste
        private readonly ConcurrentDictionary<RequestCacheKey, Func<IServiceProvider, object, CancellationToken, Task<object>>> _asyncRequestHandlerCache = new();

        // Cache per i delegati degli handler asincroni delle notifiche
        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> _asyncNotificationHandlerCache = new();


        // Statistiche della cache per diagnostica
        private int _requestHits;
        private int _requestMisses;
        private int _notificationHits;
        private int _notificationMisses;
        private int _asyncRequestHits;
        private int _asyncRequestMisses;
        private int _asyncNotificationHits;
        private int _asyncNotificationMisses;

        // Singleton
        public static DelegateCache Instance => _instance.Value;

        private DelegateCache() { }

        /// <summary>
        /// Chiave composita per la cache delle richieste
        /// </summary>
        public readonly struct RequestCacheKey : IEquatable<RequestCacheKey>
        {
            public Type RequestType { get; }
            public Type ResponseType { get; }

            public RequestCacheKey(Type requestType, Type responseType)
            {
                RequestType = requestType;
                ResponseType = responseType;
            }

            public bool Equals(RequestCacheKey other)
            {
                return RequestType == other.RequestType && ResponseType == other.ResponseType;
            }

            public override bool Equals(object obj)
            {
                return obj is RequestCacheKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(RequestType, ResponseType);
            }
        }

        /// <summary>
        /// Statistiche di utilizzo della cache delle richieste
        /// </summary>
        public (int Hits, int Misses) RequestStats => (_requestHits, _requestMisses);

        /// <summary>
        /// Statistiche di utilizzo della cache delle notifiche
        /// </summary>
        public (int Hits, int Misses) NotificationStats => (_notificationHits, _notificationMisses);

        /// <summary>
        /// Dimensione della cache degli handler delle richieste
        /// </summary>
        public int RequestHandlerCacheSize => _requestHandlerCache.Count;

        /// <summary>
        /// Dimensione della cache degli handler delle notifiche
        /// </summary>
        public int NotificationHandlerCacheSize => _notificationHandlerCache.Count;

        /// <summary>
        /// Ottiene un delegato esistente o ne crea uno nuovo per un handler di richieste
        /// </summary>
        public Func<IServiceProvider, object, object> GetOrCreateRequestHandler<TRequest, TResponse>(
            Func<Func<IServiceProvider, object, object>> factory)
            where TRequest : IRequest<TResponse>
        {
            var key = new RequestCacheKey(typeof(TRequest), typeof(TResponse));

            if (_requestHandlerCache.TryGetValue(key, out var cachedHandler))
            {
                Interlocked.Increment(ref _requestHits);
                return cachedHandler;
            }

            Interlocked.Increment(ref _requestMisses);
            var handler = factory();
            _requestHandlerCache[key] = handler;
            return handler;
        }

        /// <summary>
        /// Ottiene un delegato esistente o ne crea uno nuovo per un handler di notifiche
        /// </summary>
        public Action<IServiceProvider, object> GetOrCreateNotificationHandler<TNotification>(
            Func<Action<IServiceProvider, object>> factory)
            where TNotification : INotification
        {
            var key = typeof(TNotification);

            if (_notificationHandlerCache.TryGetValue(key, out var cachedHandler))
            {
                Interlocked.Increment(ref _notificationHits);
                return cachedHandler;
            }

            Interlocked.Increment(ref _notificationMisses);
            var handler = factory();
            _notificationHandlerCache[key] = handler;
            return handler;
        }


        /// <summary>
        /// Statistiche di utilizzo della cache degli handler asincroni delle richieste
        /// </summary>
        public (int Hits, int Misses) AsyncRequestStats => (_asyncRequestHits, _asyncRequestMisses);

        /// <summary>
        /// Statistiche di utilizzo della cache degli handler asincroni delle notifiche
        /// </summary>
        public (int Hits, int Misses) AsyncNotificationStats => (_asyncNotificationHits, _asyncNotificationMisses);

        /// <summary>
        /// Dimensione della cache degli handler asincroni delle richieste
        /// </summary>
        public int AsyncRequestHandlerCacheSize => _asyncRequestHandlerCache.Count;

        /// <summary>
        /// Dimensione della cache degli handler asincroni delle notifiche
        /// </summary>
        public int AsyncNotificationHandlerCacheSize => _asyncNotificationHandlerCache.Count;

        /// <summary>
        /// Ottiene un delegato esistente o ne crea uno nuovo per un handler asincrono di richieste
        /// </summary>
        public Func<IServiceProvider, object, CancellationToken, Task<object>> GetOrCreateAsyncRequestHandler<TRequest, TResponse>(
            Func<Func<IServiceProvider, object, CancellationToken, Task<object>>> factory)
            where TRequest : IAsyncRequest<TResponse>
        {
            var key = new RequestCacheKey(typeof(TRequest), typeof(TResponse));

            if (_asyncRequestHandlerCache.TryGetValue(key, out var cachedHandler))
            {
                Interlocked.Increment(ref _asyncRequestHits);
                return cachedHandler;
            }

            Interlocked.Increment(ref _asyncRequestMisses);
            var handler = factory();
            _asyncRequestHandlerCache[key] = handler;
            return handler;
        }

        /// <summary>
        /// Ottiene un delegato esistente o ne crea uno nuovo per un handler asincrono di notifiche
        /// </summary>
        public Func<IServiceProvider, object, CancellationToken, Task> GetOrCreateAsyncNotificationHandler<TNotification>(
            Func<Func<IServiceProvider, object, CancellationToken, Task>> factory)
            where TNotification : IAsyncNotification
        {
            var key = typeof(TNotification);

            if (_asyncNotificationHandlerCache.TryGetValue(key, out var cachedHandler))
            {
                Interlocked.Increment(ref _asyncNotificationHits);
                return cachedHandler;
            }

            Interlocked.Increment(ref _asyncNotificationMisses);
            var handler = factory();
            _asyncNotificationHandlerCache[key] = handler;
            return handler;
        }




        /// <summary>
        /// Svuota la cache e azzera le statistiche
        /// </summary>
        public void ClearCache()
        {
            _requestHandlerCache.Clear();
            _notificationHandlerCache.Clear();
            _requestHits = 0;
            _requestMisses = 0;
            _notificationHits = 0;
            _notificationMisses = 0;
            _asyncRequestHandlerCache.Clear();
            _asyncNotificationHandlerCache.Clear();
            _asyncRequestHits = 0;
            _asyncRequestMisses = 0;
            _asyncNotificationHits = 0;
            _asyncNotificationMisses = 0;
        }
    }
}