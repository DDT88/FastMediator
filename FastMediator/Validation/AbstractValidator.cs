using FastMediator.Interfaces;

namespace FastMediator.Validation
{
    /// <summary>
    /// Classe base per i validatori
    /// </summary>
    /// <typeparam name="T">Il tipo da validare</typeparam>
    public abstract class AbstractValidator<T> : IValidator<T>
    {
        /// <summary>
        /// Valida l'oggetto specificato
        /// </summary>
        /// <param name="instance">L'oggetto da validare</param>
        /// <returns>Il risultato della validazione</returns>
        public ValidationResult Validate(T instance)
        {
            var result = new ValidationResult();
            ValidateInternal(instance, result);
            return result;
        }

        /// <summary>
        /// Implementazione della validazione da sovrascrivere nelle classi derivate
        /// </summary>
        /// <param name="instance">L'oggetto da validare</param>
        /// <param name="result">Il risultato di validazione da popolare</param>
        protected abstract void ValidateInternal(T instance, ValidationResult result);
    }
}