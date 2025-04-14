using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Configuration
{
    /// <summary>
    /// Opzioni di configurazione per FastMediator
    /// </summary>
    public class FastMediatorOptions
    {
        /// <summary>
        /// Indica se i behavior diagnostici sono abilitati
        /// </summary>
        public bool EnableDiagnostics { get; set; } = false;

        /// <summary>
        /// Indica se il timing delle operazioni è abilitato
        /// </summary>
        public bool EnableTiming { get; set; } = false;


        /// <summary>
        /// Indica se il logging dettagliato è abilitato
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
    }
}
