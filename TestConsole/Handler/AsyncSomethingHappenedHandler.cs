using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.Request;

namespace TestConsole.Handler
{
    // Handler asincrono per la notifica
    public class AsyncSomethingHappenedHandler : IAsyncNotificationHandler<AsyncSomethingHappened>
    {
        public Task HandleAsync(AsyncSomethingHappened notification, CancellationToken cancellationToken = default)
        {
            // Intenzionalmente vuoto per il benchmark
            return Task.CompletedTask;
        }
    }
}
