namespace FastMediator.Validation
{
    /// <summary>
    /// Rappresenta un errore di validazione
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Il nome della proprietà che ha fallito la validazione
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Il messaggio di errore
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Crea un nuovo errore di validazione
        /// </summary>
        /// <param name="propertyName">Il nome della proprietà</param>
        /// <param name="errorMessage">Il messaggio di errore</param>
        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
}