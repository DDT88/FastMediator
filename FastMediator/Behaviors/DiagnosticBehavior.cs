using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che fornisce informazioni diagnostiche sulla pipeline
    /// </summary>
    public class DiagnosticBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        private readonly IServiceProvider _serviceProvider;

        public DiagnosticBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public int Order => 999; // Priorità molto bassa, eseguito per ultimo

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            var behaviors = _serviceProvider.GetServices(typeof(IPipelineBehavior<TRequest, TResponse>))
                .Cast<IPipelineBehavior<TRequest, TResponse>>()
                .ToList();

            Console.WriteLine($"\n----- PIPELINE PER {typeof(TRequest).Name} -----");
            Console.WriteLine($"Behaviors registrati: {behaviors.Count}");

            foreach (var behavior in behaviors)
            {
                if (behavior == this) continue; // Salta questo behavior stesso

                var orderInfo = behavior is IOrderedPipelineBehavior ordered
                    ? $" (Ordine: {ordered.Order})"
                    : "";

                Console.WriteLine($"- {behavior.GetType().Name}{orderInfo}");
            }

            Console.WriteLine("----- FINE DIAGNOSTICA -----\n");

            return next(request);
        }
    }
}
