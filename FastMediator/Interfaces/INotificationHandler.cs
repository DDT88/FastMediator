using System;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Gestisce una notifica di tipo TNotification
    /// </summary>
    /// <typeparam name="TNotification">Il tipo di notifica da gestire</typeparam>
    public interface INotificationHandler<TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Gestisce la notifica specificata
        /// </summary>
        /// <param name="notification">La notifica da gestire</param>
        void Handle(TNotification notification);
    }
}