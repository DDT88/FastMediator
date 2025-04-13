using System;
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
            return (provider, notification) =>
            {
                // Ottieni tutti gli handler per questa notifica
                var handlers = provider.GetServices<INotificationHandler<TNotification>>();

                // Chiama tutti gli handler
                foreach (var handler in handlers)
                {
                    handler.Handle((TNotification)notification);
                }
            };
        }
    }
}
