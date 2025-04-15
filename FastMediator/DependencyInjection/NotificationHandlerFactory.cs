using System;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    // Classe helper tipizzata per ogni tipo di notifica
    public class NotificationHandlerFactory<TNotification>
        where TNotification : INotification
    {
        // Metodo statico che crea il delegato per l'handler
        public static Action<IServiceProvider, object> CreateHandler()
        {
            return DelegateCache.Instance.GetOrCreateNotificationHandler<TNotification>(() =>
            {
                return (provider, notification) =>
                {
                    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                    // Crea un nuovo scope
                    using (var scope = scopeFactory.CreateScope())
                    {
                        // Ottieni tutti gli handler per questa notifica dallo scope creato
                        var handlers = scope.ServiceProvider.GetServices<INotificationHandler<TNotification>>();

                        // Chiama tutti gli handler
                        foreach (var handler in handlers)
                        {
                            handler.Handle((TNotification)notification);
                        }
                    }
                };
            });
        }
    }
}
