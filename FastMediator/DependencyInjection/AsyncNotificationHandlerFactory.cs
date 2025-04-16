using System;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    // Classe helper tipizzata per ogni tipo di notifica asincrona
    public class AsyncNotificationHandlerFactory<TNotification>
        where TNotification : IAsyncNotification
    {
        // Metodo statico che crea il delegato per l'handler asincrono
        public static Func<IServiceProvider, object, CancellationToken, Task> CreateHandler()
        {
            return DelegateCache.Instance.GetOrCreateAsyncNotificationHandler<TNotification>(() =>
            {
                return async (provider, notification, cancellationToken) =>
                {
                    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                    // Crea un nuovo scope
                    using (var scope = scopeFactory.CreateScope())
                    {
                        // Ottieni tutti gli handler per questa notifica dallo scope creato
                        var handlers = scope.ServiceProvider.GetServices<IAsyncNotificationHandler<TNotification>>();

                        // Crea task per tutti gli handler
                        var tasks = new List<Task>();
                        foreach (var handler in handlers)
                        {
                            tasks.Add(handler.HandleAsync((TNotification)notification, cancellationToken));
                        }

                        // Attendi il completamento di tutti gli handler
                        await Task.WhenAll(tasks);
                    }
                };
            });
        }
    }
}