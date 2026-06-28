using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    /// <summary>
    /// Classe helper tipizzata per ogni tipo di notifica asincrona. Risolve gli handler
    /// dallo scope corrente (ambient) senza creare un nuovo scope.
    /// </summary>
    public class AsyncNotificationHandlerFactory<TNotification>
        where TNotification : IAsyncNotification
    {
        /// <summary>
        /// Crea (o recupera dalla cache) il delegato per gli handler della notifica asincrona.
        /// </summary>
        public static Func<IServiceProvider, object, CancellationToken, Task> CreateHandler()
        {
            return DelegateCache.Instance.GetOrCreateAsyncNotificationHandler<TNotification>(() =>
            {
                return (provider, notification, cancellationToken) =>
                {
                    var handlers = provider.GetServices<IAsyncNotificationHandler<TNotification>>();

                    var tasks = new List<Task>();
                    foreach (var handler in handlers)
                    {
                        tasks.Add(handler.HandleAsync((TNotification)notification, cancellationToken));
                    }

                    return Task.WhenAll(tasks);
                };
            });
        }
    }
}
