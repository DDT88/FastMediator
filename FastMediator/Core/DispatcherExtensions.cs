using FastMediator.Core;
using FastMediator.Interfaces;

namespace FastMediator.Core
{
    /// <summary>
    /// Estensioni per il dispatcher
    /// </summary>
    public static class DispatcherExtensions
    {
        /// <summary>
        /// Invia una richiesta fortemente tipizzata
        /// </summary>
        public static TResponse Send<TRequest, TResponse>(this Dispatcher dispatcher, TRequest request)
            where TRequest : IRequest<TResponse>
        {
            return dispatcher.Send<TResponse>(request);
        }

        /// <summary>
        /// Pubblica una notifica fortemente tipizzata
        /// </summary>
        public static void Publish<TNotification>(this Dispatcher dispatcher, TNotification notification)
            where TNotification : INotification
        {
            dispatcher.Publish(notification);
        }
    }
}