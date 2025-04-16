using FastMediator.Configuration;
using FastMediator.Interfaces;
using FastMediator.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che registra i log dettagliati delle richieste asincrone
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta</typeparam>
    public class LoggingBehaviorAsync<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IAsyncRequest<TResponse>
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger _logger;
        private readonly FastMediatorOptions _options;

        /// <summary>
        /// Inizializza una nuova istanza del behavior di logging asincrono
        /// </summary>
        public LoggingBehaviorAsync(FastMediatorOptions options)
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                MaxDepth = 10,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _options = options;

            _logger = MediatorLoggerFactory.CreateLogger<LoggingBehaviorAsync<TRequest, TResponse>>(options);
        }

        public int Order => 10; // Priorità media-alta, eseguito abbastanza presto ma dopo la validazione

        public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
        {
            var requestType = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString("N"); // ID univoco per tracciare la richiesta

            // Log della richiesta
            _logger.LogInformation($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LoggingBehaviorAsync] [Request {requestId}] {requestType}");
            _logger.LogInformation($"Request Details:");

            try
            {
                // Serializza la richiesta in JSON per visualizzare tutti i campi
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                _logger.LogInformation(requestJson);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Impossibile serializzare la richiesta: {ex.Message}");
                // Fallback: stampa le proprietà manualmente
                PrintObjectProperties(request);
            }

            // Esegui l'handler e misura il tempo
            var startTime = DateTime.Now;
            TResponse response;

            try
            {
                response = await next(request, cancellationToken);

                // Log della risposta
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;

                _logger.LogInformation($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LoggingBehaviorAsync] [Response {requestId}] {requestType} - Completata in {duration:F2}ms");

                // Se la risposta non è void o unit, logga anche i dettagli della risposta
                if (typeof(TResponse) != typeof(System.Threading.Tasks.Task) &&
                    !IsUnitType(typeof(TResponse)))
                {
                    _logger.LogInformation("Response Details:");
                    try
                    {
                        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                        _logger.LogInformation(responseJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[ERROR] Impossibile serializzare la risposta: {ex.Message}");
                        PrintObjectProperties(response);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log dell'eccezione
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;

                _logger.LogError($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LoggingBehaviorAsync] [Exception {requestId}] {requestType} - Fallita dopo {duration:F2}ms");
                _logger.LogError($"Exception: {ex.GetType().Name}: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");

                // Rilancia l'eccezione
                throw;
            }

            return response;
        }

        private bool IsUnitType(Type type)
        {
            // Verifica se il tipo è uno dei tipi "unit" comuni
            return type == typeof(void) ||
                   type == typeof(Task) ||
                   type == typeof(ValueTuple) ||
                   type.Name == "Unit"; // Per unit types personalizzati
        }

        private void PrintObjectProperties(object obj)
        {
            if (obj == null)
            {
                _logger.LogInformation("null");
                return;
            }

            var properties = obj.GetType().GetProperties();
            var builder = new StringBuilder();

            builder.AppendLine("{");
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    builder.AppendLine($"  \"{prop.Name}\": \"{value}\"");
                }
                catch
                {
                    builder.AppendLine($"  \"{prop.Name}\": \"[Errore durante l'accesso al valore]\"");
                }
            }
            builder.AppendLine("}");

            _logger.LogInformation(builder.ToString());
        }
    }
}