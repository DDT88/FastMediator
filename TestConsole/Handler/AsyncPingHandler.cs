using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.Request;

namespace TestConsole.Handler
{
    public class AsyncPingHandler : IAsyncRequestHandler<AsyncPingRequest, string>
    {
        public Task<string> HandleAsync(AsyncPingRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"Risposta asincrona a: {request.Message}");
        }
    }
}
