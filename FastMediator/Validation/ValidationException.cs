using System;
using System.Collections.Generic;
using System.Linq;

namespace FastMediator.Validation
{
    /// <summary>
    /// Eccezione lanciata quando la validazione fallisce
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Errori di validazione
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; }

        /// <summary>
        /// Crea una nuova eccezione di validazione
        /// </summary>
        /// <param name="errors">Gli errori di validazione</param>
        public ValidationException(IEnumerable<ValidationError> errors)
            : base(CreateErrorMessage(errors))
        {
            Errors = errors?.ToList() ?? new List<ValidationError>();
        }

        /// <summary>
        /// Crea una nuova eccezione di validazione
        /// </summary>
        /// <param name="message">Il messaggio di errore</param>
        public ValidationException(string message)
            : base(message)
        {
            Errors = new List<ValidationError>();
        }

        /// <summary>
        /// Crea una nuova eccezione di validazione
        /// </summary>
        /// <param name="message">Il messaggio di errore</param>
        /// <param name="errors">Gli errori di validazione</param>
        public ValidationException(string message, IEnumerable<ValidationError> errors)
            : base(message)
        {
            Errors = errors?.ToList() ?? new List<ValidationError>();
        }

        /// <summary>
        /// Crea un messaggio di errore basato sugli errori di validazione
        /// </summary>
        private static string CreateErrorMessage(IEnumerable<ValidationError> errors)
        {
            var errorList = errors?.ToList() ?? new List<ValidationError>();

            if (!errorList.Any())
                return "Validation failed.";

            var errorMessages = errorList
                .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                .ToList();

            return $"Validation failed: {string.Join("; ", errorMessages)}";
        }
    }
}