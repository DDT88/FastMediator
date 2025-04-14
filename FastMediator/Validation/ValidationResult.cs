using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastMediator.Validation
{
    /// <summary>
    /// Rappresenta il risultato di una validazione
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationError> _errors = new();

        /// <summary>
        /// Indica se la validazione è stata superata
        /// </summary>
        public bool IsValid => !_errors.Any();

        /// <summary>
        /// Gli errori rilevati durante la validazione
        /// </summary>
        public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Aggiunge un errore alla lista
        /// </summary>
        public void AddError(string propertyName, string errorMessage)
        {
            _errors.Add(new ValidationError(propertyName, errorMessage));
        }

        /// <summary>
        /// Aggiunge un errore generico (non associato a una proprietà specifica)
        /// </summary>
        public void AddError(string errorMessage)
        {
            _errors.Add(new ValidationError(string.Empty, errorMessage));
        }
    }

    /// <summary>
    /// Rappresenta un singolo errore di validazione
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Il nome della proprietà che ha causato l'errore
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Il messaggio di errore
        /// </summary>
        public string ErrorMessage { get; }

        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
}
