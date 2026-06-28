using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Configuration;
using FastMediator.DependencyInjection;

namespace FastMediator.Core
{
    /// <summary>
    /// Contiene le mappe dei delegati handler precompilati. È condiviso (singleton) tra
    /// tutte le istanze <see cref="Dispatcher"/> (registrate come scoped), così la
    /// compilazione dei delegati avviene una sola volta mentre la risoluzione degli
    /// handler usa lo scope corrente.
    /// </summary>
    public sealed class DispatcherRegistry
    {
        // Mappe popolate all'avvio (sola lettura dopo la costruzione).
        private readonly Dictionary<Type, Func<IServiceProvider, object, object>> _handlers;
        private readonly Dictionary<Type, List<Action<IServiceProvider, object>>> _notificationHandlers;
        private readonly Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> _asyncHandlers;
        private readonly Dictionary<Type, List<Func<IServiceProvider, object, CancellationToken, Task>>> _asyncNotificationHandlers;

        // Cache per la modalità LazyLoading/Hybrid (thread-safe, condivisa tra scope).
        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, object>> _lazyHandlers = new();
        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> _lazyAsyncHandlers = new();

        /// <summary>
        /// Opzioni di configurazione del mediator.
        /// </summary>
        public FastMediatorOptions Options { get; }

        /// <summary>
        /// Inizializza il registro con le mappe precompilate.
        /// </summary>
        public DispatcherRegistry(
            Dictionary<Type, Func<IServiceProvider, object, object>> handlers,
            Dictionary<Type, List<Action<IServiceProvider, object>>> notificationHandlers,
            Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> asyncHandlers,
            Dictionary<Type, List<Func<IServiceProvider, object, CancellationToken, Task>>> asyncNotificationHandlers,
            FastMediatorOptions options)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            _notificationHandlers = notificationHandlers ?? throw new ArgumentNullException(nameof(notificationHandlers));
            _asyncHandlers = asyncHandlers ?? throw new ArgumentNullException(nameof(asyncHandlers));
            _asyncNotificationHandlers = asyncNotificationHandlers ?? throw new ArgumentNullException(nameof(asyncNotificationHandlers));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Restituisce il delegato per l'handler della richiesta indicata, compilandolo
        /// al primo utilizzo in modalità LazyLoading/Hybrid.
        /// </summary>
        public Func<IServiceProvider, object, object> GetRequestHandler(Type requestType, Type responseType)
        {
            if (_handlers.TryGetValue(requestType, out var handler))
                return handler;

            if (Options.RegistrationMode == HandlerRegistrationMode.Startup)
                throw new InvalidOperationException(
                    $"Nessun handler registrato per la richiesta '{requestType.FullName}'.");

            return _lazyHandlers.GetOrAdd(requestType, rt => BuildRequestHandler(rt, responseType));
        }

        /// <summary>
        /// Restituisce il delegato per l'handler asincrono della richiesta indicata.
        /// </summary>
        public Func<IServiceProvider, object, CancellationToken, Task<object>> GetAsyncRequestHandler(Type requestType, Type responseType)
        {
            if (_asyncHandlers.TryGetValue(requestType, out var handler))
                return handler;

            if (Options.RegistrationMode == HandlerRegistrationMode.Startup)
                throw new InvalidOperationException(
                    $"Nessun handler asincrono registrato per la richiesta '{requestType.FullName}'.");

            return _lazyAsyncHandlers.GetOrAdd(requestType, rt => BuildAsyncRequestHandler(rt, responseType));
        }

        /// <summary>
        /// Restituisce gli handler della notifica indicata, oppure null se non registrati.
        /// </summary>
        public List<Action<IServiceProvider, object>>? GetNotificationHandlers(Type notificationType)
            => _notificationHandlers.TryGetValue(notificationType, out var list) ? list : null;

        /// <summary>
        /// Restituisce gli handler asincroni della notifica indicata, oppure null se non registrati.
        /// </summary>
        public List<Func<IServiceProvider, object, CancellationToken, Task>>? GetAsyncNotificationHandlers(Type notificationType)
            => _asyncNotificationHandlers.TryGetValue(notificationType, out var list) ? list : null;

        private static Func<IServiceProvider, object, object> BuildRequestHandler(Type requestType, Type responseType)
        {
            var factoryType = typeof(RequestHandlerFactory<,>).MakeGenericType(requestType, responseType);
            var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static)!;
            return (Func<IServiceProvider, object, object>)createMethod.Invoke(null, null)!;
        }

        private static Func<IServiceProvider, object, CancellationToken, Task<object>> BuildAsyncRequestHandler(Type requestType, Type responseType)
        {
            var factoryType = typeof(AsyncRequestHandlerFactory<,>).MakeGenericType(requestType, responseType);
            var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static)!;
            return (Func<IServiceProvider, object, CancellationToken, Task<object>>)createMethod.Invoke(null, null)!;
        }
    }
}
