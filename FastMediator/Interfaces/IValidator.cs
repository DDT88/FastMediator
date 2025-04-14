using FastMediator.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Interfaces
{
    /// <summary>
    /// Definisce un validatore per un tipo specifico
    /// </summary>
    /// <typeparam name="T">Il tipo da validare</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Valida l'oggetto specificato
        /// </summary>
        /// <param name="instance">L'oggetto da validare</param>
        /// <returns>Il risultato della validazione</returns>
        ValidationResult Validate(T instance);
    }
}
