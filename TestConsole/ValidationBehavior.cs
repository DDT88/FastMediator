using FastMediator;
using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            Console.WriteLine($"[Validation] Validating {typeof(TRequest).Name}");

            // Simula errore se il messaggio è vuoto
            if (request is Ping ping && string.IsNullOrWhiteSpace(ping.Message))
            {
                throw new ArgumentException("Ping.Message non può essere vuoto!");
            }

            return next(request);
        }
    }
}
