using FastMediator.Configuration;
using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.DependencyInjection
{
    // Estensioni per la configurazione
    public static class FastMediatorOptionsExtensions
    {
        public static FastMediatorOptions UseHybridMode(this FastMediatorOptions options)
        {
            options.RegistrationMode = HandlerRegistrationMode.Hybrid;
            return options;
        }

        public static FastMediatorOptions WithWarmup<TRequest>(this FastMediatorOptions options)
    where TRequest : class  // Rimuoviamo il vincolo errato
        {
            Type requestType = typeof(TRequest);

            // Verifichiamo che sia effettivamente un IRequest<> di qualche tipo
            if (requestType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            {
                options.WarmupTypes.Add(requestType);
            }
            else
            {
                throw new ArgumentException($"Il tipo {requestType.Name} non implementa IRequest<>");
            }

            return options;
        }

        public static FastMediatorOptions WithWarmupTypes(this FastMediatorOptions options, params Type[] types)
        {
            foreach (var type in types)
            {
                if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
                {
                    options.WarmupTypes.Add(type);
                }
                else
                {
                    throw new ArgumentException($"Il tipo {type.Name} non implementa IRequest<>");
                }
            }
            return options;
        }
    }
}
