using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastMediator.Behaviors;
using FastMediator.Configuration;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrutor;

namespace FastMediator.DependencyInjection
{
    /// <summary>
    /// Estensioni per IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Aggiunge il mediator personalizzato con tutti gli handler registrati tramite scan
        /// </summary>
        public static IServiceCollection AddCustomMediator(this IServiceCollection services , Action<FastMediatorOptions> configureOptions = null)
        {
            return AddCustomMediator(services, scan => scan.FromApplicationDependencies() ,configureOptions);
        }



        /// <summary>
        /// Aggiunge il mediator personalizzato specificando esplicitamente un ILoggerFactory
        /// </summary>
        public static IServiceCollection AddCustomMediator(
            this IServiceCollection services,
            ILoggerFactory loggerFactory,
            Action<FastMediatorOptions> configureOptions = null)
        {
            return AddCustomMediator(services, scan => scan.FromApplicationDependencies(), options =>
            {
                options.LoggerFactory = loggerFactory;
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Aggiunge il mediator personalizzato con configurazione personalizzata per lo scanner e un ILoggerFactory esplicito
        /// </summary>
        public static IServiceCollection AddCustomMediator(
            this IServiceCollection services,
            Func<ITypeSourceSelector, IImplementationTypeSelector> configureScanner,
            ILoggerFactory loggerFactory,
            Action<FastMediatorOptions> configureOptions = null)
        {
            return AddCustomMediator(services, configureScanner, options =>
            {
                options.LoggerFactory = loggerFactory;
                configureOptions?.Invoke(options);
            });
        }


        /// <summary>
        /// Aggiunge il mediator personalizzato con configurazione personalizzata per lo scanner
        /// </summary>
        public static IServiceCollection AddCustomMediator(this IServiceCollection services, Func<ITypeSourceSelector, IImplementationTypeSelector> configureScanner
            , Action<FastMediatorOptions> configureOptions = null)
        {
            // Configura le opzioni
            var options = new FastMediatorOptions();
            configureOptions?.Invoke(options);

            // opzioni disponibili per l'injection
            services.AddSingleton(options);

            // Registra i behavior diagnostici se abilitati
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

            services.Scan(scan =>
            {
                // Il configureScanner ora restituisce un IImplementationTypeSelector che possiamo continuare a usare
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
                   // Aggiungiamo le registrazioni per gli handler asincroni
                   .AddClasses(classes => classes.AssignableTo(typeof(IAsyncRequestHandler<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IAsyncNotificationHandler<>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehaviorAsync<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IStreamRequestHandler<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime()
                   .AddClasses(classes => classes.AssignableTo(typeof(IStreamPipelineBehavior<,>)))
                   .AsImplementedInterfaces()
                   .WithTransientLifetime();
            });

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(ValidationBehaviorAsync<,>));
            services.AddTransient(typeof(IStreamPipelineBehavior<,>), typeof(StreamValidationBehavior<,>));

            services.AddSingleton<Dispatcher>(sp =>
            {
                var handlerMap = new Dictionary<Type, Func<IServiceProvider, object, object>>();
                var notificationMap = new Dictionary<Type, List<Action<IServiceProvider, object>>>();
                var asyncHandlerMap = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>>();
                var asyncNotificationMap = new Dictionary<Type, List<Func<IServiceProvider, object, CancellationToken, Task>>>();
                var streamHandlerMap = new Dictionary<Type, Func<IServiceProvider, object, CancellationToken, object>>();

                if (options.RegistrationMode == HandlerRegistrationMode.Startup ||
                    options.RegistrationMode == HandlerRegistrationMode.Hybrid)
                {

                    // Ottieni tutti i tipi di handler registrati
                    var requestHandlerTypes = services
                        .Where(sd => sd.ServiceType.IsGenericType &&
                                     sd.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                        .ToList();

                    // Per ogni tipo di handler di richieste
                    foreach (var descriptor in requestHandlerTypes)
                    {
                        var serviceType = descriptor.ServiceType;
                        var requestType = serviceType.GenericTypeArguments[0];
                        var responseType = serviceType.GenericTypeArguments[1];

                        // Creiamo il tipo della factory
                        var factoryType = typeof(RequestHandlerFactory<,>).MakeGenericType(requestType, responseType);

                        // Chiamiamo il metodo statico CreateHandler
                        var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                        var handlerDelegate = (Func<IServiceProvider, object, object>)createMethod.Invoke(null, null);

                        // Aggiungiamo il delegato alla mappa
                        handlerMap[requestType] = handlerDelegate;
                    }
                    // Aggiungiamo gli handler asincroni
                    var asyncRequestHandlerTypes = services
                        .Where(sd => sd.ServiceType.IsGenericType &&
                                     sd.ServiceType.GetGenericTypeDefinition() == typeof(IAsyncRequestHandler<,>))
                        .ToList();

                    foreach (var descriptor in asyncRequestHandlerTypes)
                    {
                        var serviceType = descriptor.ServiceType;
                        var requestType = serviceType.GenericTypeArguments[0];
                        var responseType = serviceType.GenericTypeArguments[1];

                        // Creiamo il tipo della factory
                        var factoryType = typeof(AsyncRequestHandlerFactory<,>).MakeGenericType(requestType, responseType);

                        // Chiamiamo il metodo statico CreateHandler
                        var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                        var handlerDelegate = (Func<IServiceProvider, object, CancellationToken, Task<object>>)createMethod.Invoke(null, null);

                        // Aggiungiamo il delegato alla mappa
                        asyncHandlerMap[requestType] = handlerDelegate;
                    }

                    // Aggiungiamo gli stream handler
                    var streamRequestHandlerTypes = services
                        .Where(sd => sd.ServiceType.IsGenericType &&
                                     sd.ServiceType.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>))
                        .ToList();

                    foreach (var descriptor in streamRequestHandlerTypes)
                    {
                        var serviceType = descriptor.ServiceType;
                        var requestType = serviceType.GenericTypeArguments[0];
                        var responseType = serviceType.GenericTypeArguments[1];

                        // Creiamo il tipo della factory
                        var factoryType = typeof(StreamRequestHandlerFactory<,>).MakeGenericType(requestType, responseType);

                        // Chiamiamo il metodo statico CreateHandler
                        var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                        var handlerDelegate = (Func<IServiceProvider, object, CancellationToken, object>)createMethod.Invoke(null, null);

                        // Aggiungiamo il delegato alla mappa
                        streamHandlerMap[requestType] = handlerDelegate;
                    }
                }

                // Ottieni tutti i tipi di handler di notifiche registrati
                var notificationHandlerTypes = services
                    .Where(sd => sd.ServiceType.IsGenericType &&
                                 sd.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .ToList();

                // Per ogni tipo di handler di notifiche
                foreach (var descriptor in notificationHandlerTypes)
                {
                    var notificationType = descriptor.ServiceType.GenericTypeArguments[0];

                    // Creiamo il tipo della factory
                    var factoryType = typeof(NotificationHandlerFactory<>).MakeGenericType(notificationType);

                    // Chiamiamo il metodo statico CreateHandler
                    var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                    var handlerAction = (Action<IServiceProvider, object>)createMethod.Invoke(null, null);

                    // Aggiungiamo l'azione alla mappa
                    if (!notificationMap.ContainsKey(notificationType))
                        notificationMap[notificationType] = new List<Action<IServiceProvider, object>>();

                    notificationMap[notificationType].Add(handlerAction);
                }

                // Ottieni tutti i tipi di handler di notifiche asincrone registrati
                var asyncNotificationHandlerTypes = services
                    .Where(sd => sd.ServiceType.IsGenericType &&
                                 sd.ServiceType.GetGenericTypeDefinition() == typeof(IAsyncNotificationHandler<>))
                    .ToList();

                // Per ogni tipo di handler di notifiche asincrone
                foreach (var descriptor in asyncNotificationHandlerTypes)
                {
                    var notificationType = descriptor.ServiceType.GenericTypeArguments[0];

                    // Creiamo il tipo della factory
                    var factoryType = typeof(AsyncNotificationHandlerFactory<>).MakeGenericType(notificationType);

                    // Chiamiamo il metodo statico CreateHandler
                    var createMethod = factoryType.GetMethod("CreateHandler", BindingFlags.Public | BindingFlags.Static);
                    var handlerFunc = (Func<IServiceProvider, object, CancellationToken, Task>)createMethod.Invoke(null, null);

                    // Aggiungiamo la funzione alla mappa
                    if (!asyncNotificationMap.ContainsKey(notificationType))
                        asyncNotificationMap[notificationType] = new List<Func<IServiceProvider, object, CancellationToken, Task>>();

                    asyncNotificationMap[notificationType].Add(handlerFunc);
                }

                return new Dispatcher(sp, handlerMap, notificationMap, asyncHandlerMap, asyncNotificationMap, streamHandlerMap, options);
            });

            return services;
        }
    }
}