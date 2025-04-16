using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastMediator.Caching;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FastMediator.DependencyInjection
{
    /// <summary>
    /// Contiene la logica per la compilazione dei delegati degli handler
    /// </summary>
    internal static class HandlerCompilation
    {
        /// <summary>
        /// Crea un delegato tipizzato per l'handler di una richiesta
        /// </summary>
        internal static Func<IServiceProvider, object, object> CreateTypedRequestHandler<TRequest, TResponse>(IServiceProvider serviceProvider)
            where TRequest : IRequest<TResponse>
        {
            return DelegateCache.Instance.GetOrCreateRequestHandler<TRequest, TResponse>(
                () => BuildRequestHandlerDelegate<TRequest, TResponse>());
        }

        /// <summary>
        /// Crea un delegato tipizzato per l'handler di una notifica
        /// </summary>
        internal static Action<IServiceProvider, object> CreateTypedNotificationHandler<TNotification>(IServiceProvider serviceProvider)
            where TNotification : INotification
        {
            return DelegateCache.Instance.GetOrCreateNotificationHandler<TNotification>(
                () => BuildNotificationHandlerDelegate<TNotification>());
        }

        /// <summary>
        /// Crea un delegato tipizzato utilizzando il factory pattern con una classe helper
        /// </summary>
        internal static Func<IServiceProvider, object, object> CreateRequestHandlerWithTypedFactory(
            IServiceProvider serviceProvider, Type requestType, Type responseType)
        {
            // Creiamo un contesto fortemente tipizzato per evitare ambiguità nella reflection
            var contextType = typeof(RequestHandlerFactory<,>).MakeGenericType(requestType, responseType);
            var context = Activator.CreateInstance(contextType);

            var factoryMethod = contextType.GetMethod("CreateHandler");
            return (Func<IServiceProvider, object, object>)factoryMethod.Invoke(context, new object[] { serviceProvider });
        }

        /// <summary>
        /// Crea un delegato tipizzato per l'handler di notifica utilizzando il factory pattern
        /// </summary>
        internal static Action<IServiceProvider, object> CreateNotificationHandlerWithTypedFactory(
            IServiceProvider serviceProvider, Type notificationType)
        {
            // Creiamo un contesto fortemente tipizzato per evitare ambiguità nella reflection
            var contextType = typeof(NotificationHandlerFactory<>).MakeGenericType(notificationType);
            var context = Activator.CreateInstance(contextType);

            var factoryMethod = contextType.GetMethod("CreateHandler");
            return (Action<IServiceProvider, object>)factoryMethod.Invoke(context, new object[] { serviceProvider });
        }

        /// <summary>
        /// Factory class per creare handler tipizzati per le richieste
        /// </summary>
        private class RequestHandlerFactory<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            public Func<IServiceProvider, object, object> CreateHandler(IServiceProvider serviceProvider)
            {
                return DelegateCache.Instance.GetOrCreateRequestHandler<TRequest, TResponse>(
                    () => BuildRequestHandlerDelegate<TRequest, TResponse>());
            }
        }

        /// <summary>
        /// Factory class per creare handler tipizzati per le notifiche
        /// </summary>
        private class NotificationHandlerFactory<TNotification>
            where TNotification : INotification
        {
            public Action<IServiceProvider, object> CreateHandler(IServiceProvider serviceProvider)
            {
                return DelegateCache.Instance.GetOrCreateNotificationHandler<TNotification>(
                    () => BuildNotificationHandlerDelegate<TNotification>());
            }
        }

        /// <summary>
        /// Costruisce un delegato per l'handler di una richiesta
        /// </summary>
        private static Func<IServiceProvider, object, object> BuildRequestHandlerDelegate<TRequest, TResponse>()
            where TRequest : IRequest<TResponse>
        {
            var handlerType = typeof(IRequestHandler<TRequest, TResponse>);
            var pipelineBehaviorType = typeof(IPipelineBehavior<TRequest, TResponse>);

            // Compila un delegato fortemente tipizzato usando Expression Trees
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var requestParam = Expression.Parameter(typeof(object), "request");

            // Cast del parametro request a TRequest
            var typedRequestParam = Expression.Convert(requestParam, typeof(TRequest));

            // Chiamata a GetRequiredService<IRequestHandler<TRequest, TResponse>>
            var getServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService")
                .MakeGenericMethod(handlerType);
            var getHandlerCall = Expression.Call(getServiceMethod, providerParam);

            // Cast del risultato a IRequestHandler<TRequest, TResponse>
            var handlerVar = Expression.Variable(handlerType, "handler");
            var assignHandler = Expression.Assign(handlerVar, getHandlerCall);

            // Chiamata a handler.Handle(request)
            var handleMethod = handlerType.GetMethod("Handle");
            var handleCall = Expression.Call(handlerVar, handleMethod, typedRequestParam);

            // GetServices per i behaviors
            var getServicesMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetServices")
                .MakeGenericMethod(pipelineBehaviorType);
            var getBehaviorsCall = Expression.Call(getServicesMethod, providerParam);

            // ToArray() sui behaviors
            var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")
                .MakeGenericMethod(pipelineBehaviorType);
            var behaviorsArrayExpr = Expression.Call(toArrayMethod, getBehaviorsCall);

            // Variabile per l'array di behaviors
            var behaviorsVar = Expression.Variable(pipelineBehaviorType.MakeArrayType(), "behaviors");
            var assignBehaviors = Expression.Assign(behaviorsVar, behaviorsArrayExpr);

            // Controllo se ci sono behaviors
            var hasBehaviorsCheck = Expression.NotEqual(
                Expression.Property(behaviorsVar, "Length"),
                Expression.Constant(0)
            );

            // Variabile per il risultato
            var resultVar = Expression.Variable(typeof(TResponse), "result");

            // Chiamata diretta all'handler se non ci sono behaviors
            var directCall = Expression.Block(
                Expression.Assign(resultVar, handleCall),
                resultVar
            );

            // Creazione della pipeline per i behaviors
            var pipelineExpr = CreatePipelineExpression<TRequest, TResponse>(behaviorsVar, handlerVar, typedRequestParam, resultVar);

            // Condizionale per scegliere tra chiamata diretta e pipeline
            var conditionalExpr = Expression.IfThenElse(
                hasBehaviorsCheck,
                pipelineExpr,
                directCall
            );

            // Blocco completo
            var blockExpr = Expression.Block(
                new[] { handlerVar, behaviorsVar, resultVar },
                assignHandler,
                assignBehaviors,
                conditionalExpr
            );

            // Cast del risultato a object
            var resultCastExpr = Expression.Convert(blockExpr, typeof(object));

            // Lambda finale
            var lambda = Expression.Lambda<Func<IServiceProvider, object, object>>(
                resultCastExpr,
                providerParam,
                requestParam
            );

            // Compila
            return lambda.Compile();
        }

        /// <summary>
        /// Crea un'espressione per la pipeline dei behaviors
        /// </summary>
        private static Expression CreatePipelineExpression<TRequest, TResponse>(
            ParameterExpression behaviorsVar,
            ParameterExpression handlerVar,
            Expression requestParam,
            ParameterExpression resultVar)
            where TRequest : IRequest<TResponse>
        {
            var pipelineBehaviorType = typeof(IPipelineBehavior<TRequest, TResponse>);
            var handleMethod = pipelineBehaviorType.GetMethod("Handle");

            // Variabili per il loop
            var indexVar = Expression.Variable(typeof(int), "i");
            var behaviorVar = Expression.Variable(pipelineBehaviorType, "currentBehavior");
            var nextDelegateType = typeof(Func<TRequest, TResponse>);
            var nextDelegateVar = Expression.Variable(nextDelegateType, "nextDelegate");

            // Lambda per l'handler finale
            var finalHandlerLambda = Expression.Lambda<Func<TRequest, TResponse>>(
                Expression.Call(handlerVar, handlerVar.Type.GetMethod("Handle"), requestParam),
                Expression.Parameter(typeof(TRequest), "req")
            );

            // Inizializzazioni
            var initIndex = Expression.Assign(indexVar, Expression.Constant(0));
            var assignFinalHandler = Expression.Assign(nextDelegateVar, finalHandlerLambda);

            // Condizione del loop
            var loopCondition = Expression.LessThan(indexVar, Expression.Property(behaviorsVar, "Length"));

            // Corpo del loop
            var getBehavior = Expression.Assign(
                behaviorVar,
                Expression.Convert(
                    Expression.ArrayIndex(behaviorsVar, indexVar),
                    pipelineBehaviorType
                )
            );

            // Parametro per la closure
            var nextDelegateParam = Expression.Parameter(typeof(TRequest), "req");

            // Chiamata al delegate corrente
            var closureBody = Expression.Invoke(nextDelegateVar, nextDelegateParam);
            var newNextDelegate = Expression.Lambda<Func<TRequest, TResponse>>(closureBody, nextDelegateParam);

            // Chiamata al behavior corrente
            var callBehavior = Expression.Call(
                behaviorVar,
                handleMethod,
                requestParam,
                newNextDelegate
            );

            // Aggiornamento del delegate per il prossimo giro
            var updateNextDelegate = Expression.Assign(
                nextDelegateVar,
                Expression.Lambda<Func<TRequest, TResponse>>(
                    callBehavior,
                    Expression.Parameter(typeof(TRequest), "req")
                )
            );

            // Incremento dell'indice
            var incrementIndex = Expression.PostIncrementAssign(indexVar);

            // Corpo completo del loop
            var loopBody = Expression.Block(
                getBehavior,
                updateNextDelegate,
                incrementIndex
            );

            // Loop completo
            var loop = Expression.Loop(
                Expression.IfThenElse(
                    loopCondition,
                    loopBody,
                    Expression.Break(Expression.Label())
                ),
                Expression.Label()
            );

            // Invoca il delegate finale
            var invokeDelegate = Expression.Assign(
                resultVar,
                Expression.Invoke(nextDelegateVar, requestParam)
            );

            // Blocco completo
            return Expression.Block(
                new[] { indexVar, behaviorVar, nextDelegateVar },
                initIndex,
                assignFinalHandler,
                loop,
                invokeDelegate,
                resultVar
            );
        }

        /// <summary>
        /// Costruisce un delegato per l'handler di una notifica
        /// </summary>
        private static Action<IServiceProvider, object> BuildNotificationHandlerDelegate<TNotification>()
            where TNotification : INotification
        {
            var handlerType = typeof(INotificationHandler<TNotification>);

            // Parametri
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var notificationParam = Expression.Parameter(typeof(object), "notification");

            // Cast della notifica al tipo corretto
            var typedNotificationParam = Expression.Convert(notificationParam, typeof(TNotification));

            // Chiamata a GetRequiredService
            var getServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService")
                .MakeGenericMethod(handlerType);
            var getHandlerCall = Expression.Call(getServiceMethod, providerParam);

            // Chiamata a handler.Handle(notification)
            var handleMethod = handlerType.GetMethod("Handle");
            var handleCall = Expression.Call(getHandlerCall, handleMethod, typedNotificationParam);

            // Lambda completa
            var lambda = Expression.Lambda<Action<IServiceProvider, object>>(
                handleCall,
                providerParam,
                notificationParam
            );

            return lambda.Compile();
        }

        //---------Async--------
        internal static Func<IServiceProvider, object, CancellationToken, Task<object>> CreateTypedAsyncRequestHandler<TRequest, TResponse>(IServiceProvider serviceProvider)
    where TRequest : IAsyncRequest<TResponse>
        {
            return DelegateCache.Instance.GetOrCreateAsyncRequestHandler<TRequest, TResponse>(
                () => BuildAsyncRequestHandlerDelegate<TRequest, TResponse>());
        }

        internal static Func<IServiceProvider, object, CancellationToken, Task> CreateTypedAsyncNotificationHandler<TNotification>(IServiceProvider serviceProvider)
            where TNotification : IAsyncNotification
        {
            return DelegateCache.Instance.GetOrCreateAsyncNotificationHandler<TNotification>(
                () => BuildAsyncNotificationHandlerDelegate<TNotification>());
        }

        internal static Func<IServiceProvider, object, CancellationToken, Task<object>> CreateAsyncRequestHandlerWithTypedFactory(
            IServiceProvider serviceProvider, Type requestType, Type responseType)
        {
            // Creiamo un contesto fortemente tipizzato per evitare ambiguità nella reflection
            var contextType = typeof(AsyncRequestHandlerFactory<,>).MakeGenericType(requestType, responseType);
            var context = Activator.CreateInstance(contextType);

            var factoryMethod = contextType.GetMethod("CreateHandler");
            return (Func<IServiceProvider, object, CancellationToken, Task<object>>)factoryMethod.Invoke(context, new object[] { serviceProvider });
        }

        internal static Func<IServiceProvider, object, CancellationToken, Task> CreateAsyncNotificationHandlerWithTypedFactory(
            IServiceProvider serviceProvider, Type notificationType)
        {
            // Creiamo un contesto fortemente tipizzato per evitare ambiguità nella reflection
            var contextType = typeof(AsyncNotificationHandlerFactory<>).MakeGenericType(notificationType);
            var context = Activator.CreateInstance(contextType);

            var factoryMethod = contextType.GetMethod("CreateHandler");
            return (Func<IServiceProvider, object, CancellationToken, Task>)factoryMethod.Invoke(context, new object[] { serviceProvider });
        }

        private static Func<IServiceProvider, object, CancellationToken, Task<object>> BuildAsyncRequestHandlerDelegate<TRequest, TResponse>()
            where TRequest : IAsyncRequest<TResponse>
        {
            var handlerType = typeof(IAsyncRequestHandler<TRequest, TResponse>);
            var pipelineBehaviorType = typeof(IPipelineBehaviorAsync<TRequest, TResponse>);

            // Parametri per il delegato
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var requestParam = Expression.Parameter(typeof(object), "request");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            // Cast del parametro request a TRequest
            var typedRequestParam = Expression.Convert(requestParam, typeof(TRequest));

            // Chiamata a GetRequiredService<IAsyncRequestHandler<TRequest, TResponse>>
            var getServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService")
                .MakeGenericMethod(handlerType);
            var getHandlerCall = Expression.Call(getServiceMethod, providerParam);

            // Cast del risultato a IAsyncRequestHandler<TRequest, TResponse>
            var handlerVar = Expression.Variable(handlerType, "handler");
            var assignHandler = Expression.Assign(handlerVar, getHandlerCall);

            // Chiamata a handler.HandleAsync(request, cancellationToken)
            var handleMethod = handlerType.GetMethod("HandleAsync");
            var handleCall = Expression.Call(handlerVar, handleMethod, typedRequestParam, cancellationTokenParam);

            // GetServices per i behaviors
            var getServicesMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetServices")
                .MakeGenericMethod(pipelineBehaviorType);
            var getBehaviorsCall = Expression.Call(getServicesMethod, providerParam);

            // ToArray() sui behaviors
            var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")
                .MakeGenericMethod(pipelineBehaviorType);
            var behaviorsArrayExpr = Expression.Call(toArrayMethod, getBehaviorsCall);

            // Variabile per l'array di behaviors
            var behaviorsVar = Expression.Variable(pipelineBehaviorType.MakeArrayType(), "behaviors");
            var assignBehaviors = Expression.Assign(behaviorsVar, behaviorsArrayExpr);

            // Controllo se ci sono behaviors
            var hasBehaviorsCheck = Expression.NotEqual(
                Expression.Property(behaviorsVar, "Length"),
                Expression.Constant(0)
            );

            // Variabile per il risultato
            var resultVar = Expression.Variable(typeof(Task<TResponse>), "result");

            // Chiamata diretta all'handler se non ci sono behaviors
            var directCall = Expression.Block(
                Expression.Assign(resultVar, handleCall),
                resultVar
            );

            // Creazione della pipeline per i behaviors
            var pipelineExpr = CreateAsyncPipelineExpression<TRequest, TResponse>(behaviorsVar, handlerVar, typedRequestParam, cancellationTokenParam, resultVar);

            // Condizionale per scegliere tra chiamata diretta e pipeline
            var conditionalExpr = Expression.IfThenElse(
                hasBehaviorsCheck,
                pipelineExpr,
                directCall
            );

            // Blocco completo
            var blockExpr = Expression.Block(
                new[] { handlerVar, behaviorsVar, resultVar },
                assignHandler,
                assignBehaviors,
                conditionalExpr
            );

            // Cast del risultato a Task<object>
            var resultCastExpr = Expression.Convert(blockExpr, typeof(Task<object>));

            // Lambda finale
            var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, Task<object>>>(
                resultCastExpr,
                providerParam,
                requestParam,
                cancellationTokenParam
            );

            // Compila
            return lambda.Compile();
        }

        private static Expression CreateAsyncPipelineExpression<TRequest, TResponse>(
            ParameterExpression behaviorsVar,
            ParameterExpression handlerVar,
            Expression requestParam,
            ParameterExpression cancellationTokenParam,
            ParameterExpression resultVar)
            where TRequest : IAsyncRequest<TResponse>
        {
            var pipelineBehaviorType = typeof(IPipelineBehaviorAsync<TRequest, TResponse>);
            var handleMethod = pipelineBehaviorType.GetMethod("HandleAsync");

            // Variabili per il loop
            var indexVar = Expression.Variable(typeof(int), "i");
            var behaviorVar = Expression.Variable(pipelineBehaviorType, "currentBehavior");
            var nextDelegateType = typeof(Func<TRequest, CancellationToken, Task<TResponse>>);
            var nextDelegateVar = Expression.Variable(nextDelegateType, "nextDelegate");

            // Lambda per l'handler finale
            var finalHandlerLambda = Expression.Lambda<Func<TRequest, CancellationToken, Task<TResponse>>>(
                Expression.Call(handlerVar, handlerVar.Type.GetMethod("HandleAsync"), requestParam, cancellationTokenParam),
                Expression.Parameter(typeof(TRequest), "req"),
                Expression.Parameter(typeof(CancellationToken), "token")
            );

            // Inizializzazioni
            var initIndex = Expression.Assign(indexVar, Expression.Constant(0));
            var assignFinalHandler = Expression.Assign(nextDelegateVar, finalHandlerLambda);

            // Condizione del loop
            var loopCondition = Expression.LessThan(indexVar, Expression.Property(behaviorsVar, "Length"));

            // Corpo del loop
            var getBehavior = Expression.Assign(
                behaviorVar,
                Expression.Convert(
                    Expression.ArrayIndex(behaviorsVar, indexVar),
                    pipelineBehaviorType
                )
            );

            // Parametri per la closure
            var nextReqParam = Expression.Parameter(typeof(TRequest), "req");
            var nextTokenParam = Expression.Parameter(typeof(CancellationToken), "token");

            // Chiamata al delegate corrente
            var closureBody = Expression.Invoke(nextDelegateVar, nextReqParam, nextTokenParam);
            var newNextDelegate = Expression.Lambda<Func<TRequest, CancellationToken, Task<TResponse>>>(closureBody, nextReqParam, nextTokenParam);

            // Chiamata al behavior corrente
            var callBehavior = Expression.Call(
                behaviorVar,
                handleMethod,
                requestParam,
                newNextDelegate,
                cancellationTokenParam
            );

            // Aggiornamento del delegate per il prossimo giro
            var updateNextDelegate = Expression.Assign(
                nextDelegateVar,
                Expression.Lambda<Func<TRequest, CancellationToken, Task<TResponse>>>(
                    callBehavior,
                    Expression.Parameter(typeof(TRequest), "req"),
                    Expression.Parameter(typeof(CancellationToken), "token")
                )
            );

            // Incremento dell'indice
            var incrementIndex = Expression.PostIncrementAssign(indexVar);

            // Corpo completo del loop
            var loopBody = Expression.Block(
                getBehavior,
                updateNextDelegate,
                incrementIndex
            );

            // Loop completo
            var loop = Expression.Loop(
                Expression.IfThenElse(
                    loopCondition,
                    loopBody,
                    Expression.Break(Expression.Label())
                ),
                Expression.Label()
            );

            // Invoca il delegate finale
            var invokeDelegate = Expression.Assign(
                resultVar,
                Expression.Invoke(nextDelegateVar, requestParam, cancellationTokenParam)
            );

            // Blocco completo
            return Expression.Block(
                new[] { indexVar, behaviorVar, nextDelegateVar },
                initIndex,
                assignFinalHandler,
                loop,
                invokeDelegate,
                resultVar
            );
        }

        private static Func<IServiceProvider, object, CancellationToken, Task> BuildAsyncNotificationHandlerDelegate<TNotification>()
            where TNotification : IAsyncNotification
        {
            var handlerType = typeof(IAsyncNotificationHandler<TNotification>);

            // Parametri
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var notificationParam = Expression.Parameter(typeof(object), "notification");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            // Cast della notifica al tipo corretto
            var typedNotificationParam = Expression.Convert(notificationParam, typeof(TNotification));

            // Chiamata a GetRequiredService
            var getServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService")
                .MakeGenericMethod(handlerType);
            var getHandlerCall = Expression.Call(getServiceMethod, providerParam);

            // Chiamata a handler.HandleAsync(notification, cancellationToken)
            var handleMethod = handlerType.GetMethod("HandleAsync");
            var handleCall = Expression.Call(getHandlerCall, handleMethod, typedNotificationParam, cancellationTokenParam);

            // Lambda completa
            var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, Task>>(
                handleCall,
                providerParam,
                notificationParam,
                cancellationTokenParam
            );

            return lambda.Compile();
        }
    }
}