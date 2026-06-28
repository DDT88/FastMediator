using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FastMediator.Behaviors;
using FastMediator.Configuration;
using FastMediator.Core;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrutor;

namespace FastMediator.DependencyInjection
{
    /// <summary>
    /// Estensioni per IServiceCollection per registrare FastMediator.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Aggiunge FastMediator con scansione di tutte le dipendenze dell'applicazione.
        /// </summary>
        public static IServiceCollection AddFastMediator(this IServiceCollection services, Action<FastMediatorOptions>? configureOptions = null)
        {
            return AddFastMediator(services, scan => scan.FromApplicationDependencies(), configureOptions);
        }

        /// <summary>
        /// Aggiunge FastMediator specificando esplicitamente un ILoggerFactory.
        /// </summary>
        public static IServiceCollection AddFastMediator(
            this IServiceCollection services,
            ILoggerFactory loggerFactory,
            Action<FastMediatorOptions>? configureOptions = null)
        {
            return AddFastMediator(services, scan => scan.FromApplicationDependencies(), options =>
            {
                options.LoggerFactory = loggerFactory;
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Aggiunge FastMediator con scanner personalizzato e un ILoggerFactory esplicito.
        /// </summary>
        public static IServiceCollection AddFastMediator(
            this IServiceCollection services,
            Func<ITypeSourceSelector, IImplementationTypeSelector> configureScanner,
            ILoggerFactory loggerFactory,
            Action<FastMediatorOptions>? configureOptions = null)
        {
            return AddFastMediator(services, configureScanner, options =>
            {
                options.LoggerFactory = loggerFactory;
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Aggiunge FastMediator con configurazione personalizzata dello scanner.
        /// </summary>
        public static IServiceCollection AddFastMediator(
            this IServiceCollection services,
            Func<ITypeSourceSelector, IImplementationTypeSelector> configureScanner,
            Action<FastMediatorOptions>? configureOptions = null)
        {
            var options = new FastMediatorOptions();
            configureOptions?.Invoke(options);

            // Le opzioni sono disponibili per l'injection.
            services.AddSingleton(options);

            // Behavior opzionali abilitati tramite opzioni.
            if (options.EnableDiagnostics)
            {
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DiagnosticBehavior<,>));
                services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(DiagnosticBehaviorAsync<,>));
            }

            if (options.EnableTiming)
            {
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TimingBehavior<,>));
                services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(TimingBehaviorAsync<,>));
            }

            if (options.EnableDetailedLogging)
            {
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(LoggingBehaviorAsync<,>));
            }

            // Scansione di handler, behaviors e validatori.
            services.Scan(scan =>
            {
                configureScanner(scan)
                   .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehavior<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IAsyncRequestHandler<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IAsyncNotificationHandler<>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehaviorAsync<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime();
            });

            // La validazione è opt-in (default abilitata). Se disabilitata, il percorso
            // caldo non passa per alcun behavior (fast-path).
            if (options.EnableValidation)
            {
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(ValidationBehaviorAsync<,>));
            }

            // Costruisce le mappe dei delegati una sola volta e le condivide tramite il registry (singleton).
            var registry = BuildRegistry(services, options);
            services.AddSingleton(registry);

            // Il Dispatcher è scoped: risolve handler/behaviors dallo scope corrente (ambient).
            services.AddScoped<Dispatcher>(sp => new Dispatcher(sp, sp.GetRequiredService<DispatcherRegistry>()));

            return services;
        }

        /// <summary>
        /// Alias obsoleto di <see cref="AddFastMediator(IServiceCollection, Action{FastMediatorOptions})"/>.
        /// </summary>
        [Obsolete("Usare AddFastMediator. Questo alias verrà rimosso in una versione futura.")]
        public static IServiceCollection AddCustomMediator(this IServiceCollection services, Action<FastMediatorOptions>? configureOptions = null)
            => AddFastMediator(services, configureOptions);

        /// <summary>
        /// Alias obsoleto di <see cref="AddFastMediator(IServiceCollection, Func{ITypeSourceSelector, IImplementationTypeSelector}, Action{FastMediatorOptions})"/>.
        /// </summary>
        [Obsolete("Usare AddFastMediator. Questo alias verrà rimosso in una versione futura.")]
        public static IServiceCollection AddCustomMediator(
            this IServiceCollection services,
            Func<ITypeSourceSelector, IImplementationTypeSelector> configureScanner,
            Action<FastMediatorOptions>? configureOptions = null)
            => AddFastMediator(services, configureScanner, configureOptions);

        private static DispatcherRegistry BuildRegistry(IServiceCollection services, FastMediatorOptions options)
        {
            var handlerMap = new Dictionary<Type, Func<IServiceProvider, object, object>>();
            var notificationMap = new Dictionary<Type, List<Action<IServiceProvider, object>>>();
            var asyncHandlerMap = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>>();
            var asyncNotificationMap = new Dictionary<Type, List<Func<IServiceProvider, object, CancellationToken, Task>>>();

            // In modalità Startup/Hybrid compiliamo subito i delegati degli handler delle richieste.
            if (options.RegistrationMode == HandlerRegistrationMode.Startup ||
                options.RegistrationMode == HandlerRegistrationMode.Hybrid)
            {
                foreach (var descriptor in GetClosedServices(services, typeof(IRequestHandler<,>)))
                {
                    var requestType = descriptor.ServiceType.GenericTypeArguments[0];
                    var responseType = descriptor.ServiceType.GenericTypeArguments[1];
                    handlerMap[requestType] = CreateDelegate<Func<IServiceProvider, object, object>>(
                        typeof(RequestHandlerFactory<,>), requestType, responseType);
                }

                foreach (var descriptor in GetClosedServices(services, typeof(IAsyncRequestHandler<,>)))
                {
                    var requestType = descriptor.ServiceType.GenericTypeArguments[0];
                    var responseType = descriptor.ServiceType.GenericTypeArguments[1];
                    asyncHandlerMap[requestType] = CreateDelegate<Func<IServiceProvider, object, CancellationToken, Task<object>>>(
                        typeof(AsyncRequestHandlerFactory<,>), requestType, responseType);
                }
            }

            // Gli handler delle notifiche vengono sempre mappati all'avvio.
            foreach (var descriptor in GetClosedServices(services, typeof(INotificationHandler<>)))
            {
                var notificationType = descriptor.ServiceType.GenericTypeArguments[0];
                var handlerAction = CreateDelegate<Action<IServiceProvider, object>>(
                    typeof(NotificationHandlerFactory<>), notificationType);

                if (!notificationMap.TryGetValue(notificationType, out var list))
                {
                    list = new List<Action<IServiceProvider, object>>();
                    notificationMap[notificationType] = list;
                }
                list.Add(handlerAction);
            }

            foreach (var descriptor in GetClosedServices(services, typeof(IAsyncNotificationHandler<>)))
            {
                var notificationType = descriptor.ServiceType.GenericTypeArguments[0];
                var handlerFunc = CreateDelegate<Func<IServiceProvider, object, CancellationToken, Task>>(
                    typeof(AsyncNotificationHandlerFactory<>), notificationType);

                if (!asyncNotificationMap.TryGetValue(notificationType, out var list))
                {
                    list = new List<Func<IServiceProvider, object, CancellationToken, Task>>();
                    asyncNotificationMap[notificationType] = list;
                }
                list.Add(handlerFunc);
            }

            return new DispatcherRegistry(handlerMap, notificationMap, asyncHandlerMap, asyncNotificationMap, options);
        }

        private static IEnumerable<ServiceDescriptor> GetClosedServices(IServiceCollection services, Type openGeneric)
        {
            return services.Where(sd =>
                sd.ServiceType.IsGenericType &&
                sd.ServiceType.GetGenericTypeDefinition() == openGeneric);
        }

        private static TDelegate CreateDelegate<TDelegate>(Type openFactory, params Type[] typeArgs)
        {
            var factoryType = openFactory.MakeGenericType(typeArgs);
            var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static)!;
            return (TDelegate)createMethod.Invoke(null, null)!;
        }
    }
}
