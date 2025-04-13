using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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
        public static IServiceCollection AddCustomMediator(this IServiceCollection services)
        {
            return AddCustomMediator(services, scan => scan.FromApplicationDependencies());
        }

        /// <summary>
        /// Aggiunge il mediator personalizzato con configurazione personalizzata per lo scanner
        /// </summary>
        public static IServiceCollection AddCustomMediator(this IServiceCollection services, Func<ITypeSourceSelector, IImplementationTypeSelector> configureScanner)
        {
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
                    .WithTransientLifetime();
            });

            services.AddSingleton<Dispatcher>(sp =>
            {
                var handlerMap = new Dictionary<Type, Func<IServiceProvider, object, object>>();
                var notificationMap = new Dictionary<Type, List<Action<IServiceProvider, object>>>();


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

                return new Dispatcher(sp, handlerMap, notificationMap);
            });

            return services;
        }
    }
}