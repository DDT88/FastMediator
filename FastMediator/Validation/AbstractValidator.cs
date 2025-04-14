using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Validation
{
    /// <summary>
    /// Classe base per implementare validatori
    /// </summary>
    public abstract class AbstractValidator<T> : IValidator<T>
    {
        public virtual ValidationResult Validate(T instance)
        {
            var result = new ValidationResult();
            Console.WriteLine($"[VALIDATE] Tipo istanza: {instance?.GetType().FullName}");
            ValidateInternal(instance, result);
            return result;
        }

        /// <summary>
        /// Implementa questo metodo per eseguire la validazione
        /// </summary>
        protected abstract void ValidateInternal(T instance, ValidationResult result);

   
    }
}
