using FastMediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FastMediator.Behaviors
{
    /// <summary>
    /// Behavior che registra informazioni dettagliate sulle richieste e le risposte
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
        where TRequest : IRequest<TResponse>
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public LoggingBehavior()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                MaxDepth = 10,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public int Order => 10; // Priorità media-alta, eseguito abbastanza presto ma dopo la validazione

        public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
        {
            var requestType = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString("N"); // ID univoco per tracciare la richiesta

            // Log della richiesta
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LoggingBehavior] [Request {requestId}] {requestType}");
            Console.WriteLine($"Request Details:");

            try
            {
                // Serializza la richiesta in JSON per visualizzare tutti i campi
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                Console.WriteLine(requestJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Impossibile serializzare la richiesta: {ex.Message}");
                // Fallback: stampa le proprietà manualmente
                PrintObjectProperties(request);
            }

            Console.WriteLine();

            // Esegui l'handler e misura il tempo
            var startTime = DateTime.Now;
            TResponse response;

            try
            {
                response = next(request);

                // Log della risposta
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LoggingBehavior] [Response {requestId}] {requestType} - Completata in {duration:F2}ms");

                // Se la risposta non è void o unit, logga anche i dettagli della risposta
                if (typeof(TResponse) != typeof(System.Threading.Tasks.Task) &&
                    !IsUnitType(typeof(TResponse)))
                {
                    Console.WriteLine("Response Details:");
                    try
                    {
                        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                        Console.WriteLine(responseJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Impossibile serializzare la risposta: {ex.Message}");
                        PrintObjectProperties(response);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log dell'eccezione
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LoggingBehavior] [Exception {requestId}] {requestType} - Fallita dopo {duration:F2}ms");
                Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // Rilancia l'eccezione
                throw;
            }

            Console.WriteLine();
            return response;
        }

        private bool IsUnitType(Type type)
        {
            // Verifica se il tipo è uno dei tipi "unit" comuni
            return type == typeof(void) ||
                   type == typeof(ValueTuple) ||
                   type.Name == "Unit"; // Per unit types personalizzati
        }

        private void PrintObjectProperties(object obj)
        {
            if (obj == null)
            {
                Console.WriteLine("null");
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

            Console.WriteLine(builder.ToString());
        }
    }
}
