using System.Collections.Generic;
using System.Linq;

namespace FastMediator.Validation
{
    /// <summary>
    /// Rappresenta il risultato di una validazione
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationError> _errors = new List<ValidationError>();

        /// <summary>
        /// Ottiene tutti gli errori di validazione
        /// </summary>
        public IReadOnlyList<ValidationError> Errors => _errors;

        /// <summary>
        /// Indica se la validazione è passata
        /// </summary>
        public bool IsValid => !_errors.Any();

        /// <summary>
        /// Aggiunge un errore di validazione
        /// </summary>
        /// <param name="propertyName">Il nome della proprietà</param>
        /// <param name="errorMessage">Il messaggio di errore</param>
        public void AddError(string propertyName, string errorMessage)
        {
            _errors.Add(new ValidationError(propertyName, errorMessage));
        }

        /// <summary>
        /// Aggiunge un errore di validazione
        /// </summary>
        /// <param name="error">L'errore di validazione</param>
        public void AddError(ValidationError error)
        {
            if (error != null)
            {
                _errors.Add(error);
            }
        }

        /// <summary>
        /// Aggiunge una collezione di errori di validazione
        /// </summary>
        /// <param name="errors">Gli errori di validazione</param>
        public void AddErrors(IEnumerable<ValidationError> errors)
        {
            if (errors != null)
            {
                _errors.AddRange(errors);
            }
        }
    }
}