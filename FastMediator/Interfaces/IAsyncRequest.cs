using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Rappresenta una richiesta asincrona che produrrà una risposta di tipo TResponse
    /// </summary>
    /// <typeparam name="TResponse">Il tipo di risposta prodotta</typeparam>
    public interface IAsyncRequest<TResponse> { }
}
