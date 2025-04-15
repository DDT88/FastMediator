using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Configuration
{
    public enum HandlerRegistrationMode
    {
        /// <summary>
        /// Registra tutti gli handler all'avvio (default, massime performance a regime)
        /// </summary>
        Startup,

        /// <summary>
        /// Registra gli handler al primo utilizzo (ottimizza tempo di avvio e memoria)
        /// </summary>
        LazyLoading,

        /// <summary>
        /// Registra all'avvio i tipi specificati, il resto al primo utilizzo
        /// </summary>
        Hybrid
    }
}
