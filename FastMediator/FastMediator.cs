using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMediator
{
    // -------- Interfaces --------
    public interface IRequest<TResponse> { }

    public interface IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        TResponse Handle(TRequest request);
    }

    public interface IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        TResponse Handle(TRequest request, Func<TRequest, TResponse> next);
    }

    public interface INotification { }

    public interface INotificationHandler<TNotification>
        where TNotification : INotification
    {
        void Handle(TNotification notification);
    }

    // -------- Dispatcher --------
    public class Dispatcher
    {
        private readonly IServiceProvider _provider;
        private readonly Dictionary<Type, Func<IServiceProvider, object, object>> _handlers;
        private readonly Dictionary<Type, List<Action<IServiceProvider, object>>> _notificationHandlers;

        public Dispatcher(IServiceProvider provider,
                          Dictionary<Type, Func<IServiceProvider, object, object>> handlers,
                          Dictionary<Type, List<Action<IServiceProvider, object>>> notificationHandlers)
        {
            _provider = provider;
            _handlers = handlers;
            _notificationHandlers = notificationHandlers;
        }

        public TResponse Send<TResponse>(IRequest<TResponse> request)
        {
            var type = request.GetType();
            if (!_handlers.TryGetValue(type, out var handler))
                throw new InvalidOperationException($"Handler not found for request type {type.Name}");

            return (TResponse)handler(_provider, request);
        }

        public void Publish<TNotification>(TNotification notification) where TNotification : INotification
        {
            var type = typeof(TNotification);
            if (_notificationHandlers.TryGetValue(type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler(_provider, notification);
                }
            }
        }
    }

    // -------- Dispatcher Extensions --------
    public static class DispatcherExtensions
    {
        public static TResponse Send<TRequest, TResponse>(this Dispatcher dispatcher, TRequest request)
            where TRequest : IRequest<TResponse>
        {
            return dispatcher.Send<TResponse>(request);
        }

        public static void Publish<TNotification>(this Dispatcher dispatcher, TNotification notification)
            where TNotification : INotification
        {
            dispatcher.Publish(notification);
        }
    }

    // -------- ServiceCollection Extensions --------
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomMediator(this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehavior<,>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            services.AddSingleton<Dispatcher>(sp =>
            {
                var handlerMap = new Dictionary<Type, Func<IServiceProvider, object, object>>();
                var notificationMap = new Dictionary<Type, List<Action<IServiceProvider, object>>>();

                var requestHandlerTypes = services
                    .Where(sd => sd.ServiceType.IsGenericType &&
                                 sd.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    .ToList();

                // Creiamo i delegati per ogni tipo di handler
                foreach (var descriptor in requestHandlerTypes)
                {
                    var serviceType = descriptor.ServiceType;
                    var requestType = serviceType.GenericTypeArguments[0];
                    var responseType = serviceType.GenericTypeArguments[1];

                    // Costruisce l'handler utilizzando il metodo generic
                    var handlerMethod = typeof(ServiceCollectionExtensions)
                        .GetMethod(nameof(BuildRequestHandlerDelegate), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(requestType, responseType);

                    var handlerDelegate = handlerMethod.Invoke(null, new object[] { sp }) as Func<IServiceProvider, object, object>;
                    handlerMap[requestType] = handlerDelegate;
                }

                // Notification handler mapping
                var notificationHandlerTypes = services
                    .Where(sd => sd.ServiceType.IsGenericType &&
                                 sd.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .ToList();

                foreach (var descriptor in notificationHandlerTypes)
                {
                    var notificationType = descriptor.ServiceType.GenericTypeArguments[0];

                    // Costruisce l'handler di notifica utilizzando il metodo generic
                    var handlerMethod = typeof(ServiceCollectionExtensions)
                        .GetMethod(nameof(BuildNotificationHandlerDelegate), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(notificationType);

                    var handlerDelegate = handlerMethod.Invoke(null, new object[] { sp }) as Action<IServiceProvider, object>;

                    if (!notificationMap.ContainsKey(notificationType))
                        notificationMap[notificationType] = new List<Action<IServiceProvider, object>>();

                    notificationMap[notificationType].Add(handlerDelegate);
                }

                return new Dispatcher(sp, handlerMap, notificationMap);
            });

            return services;
        }

        // Metodo per costruire delegati fortemente tipizzati per gli handler di richieste
        // AGGIUNTO IL VINCOLO where TRequest : IRequest<TResponse> per risolvere l'errore di compilazione
        private static Func<IServiceProvider, object, object> BuildRequestHandlerDelegate<TRequest, TResponse>(IServiceProvider serviceProvider)
            where TRequest : IRequest<TResponse>
        {
            var handlerType = typeof(IRequestHandler<TRequest, TResponse>);
            var pipelineBehaviorType = typeof(IPipelineBehavior<TRequest, TResponse>);

            return (provider, request) =>
            {
                // Ottieni tutti i behaviors per questo handler
                var behaviors = provider.GetServices(pipelineBehaviorType).ToArray();

                // Se non ci sono behaviors, esegui direttamente l'handler
                if (behaviors.Length == 0)
                {
                    var handler = provider.GetRequiredService(handlerType);
                    return ((IRequestHandler<TRequest, TResponse>)handler).Handle((TRequest)request);
                }

                // Funzione che esegue l'handler originale
                Func<TRequest, TResponse> handlerFunc = req =>
                {
                    var handler = provider.GetRequiredService(handlerType);
                    return ((IRequestHandler<TRequest, TResponse>)handler).Handle(req);
                };

                // Costruisci la pipeline di behaviors
                foreach (var behavior in behaviors.Reverse())
                {
                    var currentHandler = handlerFunc;
                    handlerFunc = req => ((IPipelineBehavior<TRequest, TResponse>)behavior).Handle(req, currentHandler);
                }

                // Esegui la pipeline completa
                return handlerFunc((TRequest)request);
            };
        }

        // Metodo per costruire delegati fortemente tipizzati per gli handler di notifiche
        private static Action<IServiceProvider, object> BuildNotificationHandlerDelegate<TNotification>(IServiceProvider serviceProvider)
            where TNotification : INotification
        {
            var handlerType = typeof(INotificationHandler<TNotification>);

            return (provider, notification) =>
            {
                var handler = provider.GetRequiredService(handlerType);
                ((INotificationHandler<TNotification>)handler).Handle((TNotification)notification);
            };
        }
    }
}