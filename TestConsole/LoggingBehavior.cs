using FastMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            Console.WriteLine($"[Logging] --> Handling {typeof(TRequest).Name}");
            var response = next(request);
            Console.WriteLine($"[Logging] <-- Handled {typeof(TRequest).Name}");
            return response;
        }
    }
}
