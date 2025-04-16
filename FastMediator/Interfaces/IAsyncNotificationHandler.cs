using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Gestisce una notifica asincrona di tipo TNotification
    /// </summary>
    /// <typeparam name="TNotification">Il tipo di notifica da gestire</typeparam>
    public interface IAsyncNotificationHandler<TNotification>
        where TNotification : IAsyncNotification
    {
        /// <summary>
        /// Gestisce la notifica specificata in modo asincrono
        /// </summary>
        /// <param name="notification">La notifica da gestire</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione</param>
        Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
    }
}