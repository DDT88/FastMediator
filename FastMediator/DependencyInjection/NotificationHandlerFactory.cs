using System;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    /// <summary>
    /// Classe helper tipizzata per ogni tipo di notifica. Risolve gli handler dallo
    /// scope corrente (ambient) senza creare un nuovo scope.
    /// </summary>
    public class NotificationHandlerFactory<TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Crea (o recupera dalla cache) il delegato per gli handler della notifica.
        /// </summary>
        public static Action<IServiceProvider, object> CreateHandler()
        {
            return DelegateCache.Instance.GetOrCreateNotificationHandler<TNotification>(() =>
            {
                return (provider, notification) =>
                {
                    var handlers = provider.GetServices<INotificationHandler<TNotification>>();
                    foreach (var handler in handlers)
                    {
                        handler.Handle((TNotification)notification);
                    }
                };
            });
        }
    }
}
