# FastMediator

[English](#english) | [Italiano](#italiano)

[![NuGet](https://img.shields.io/nuget/v/FastMediator.svg)](https://www.nuget.org/packages/FastMediator)
[![NuGet](https://img.shields.io/nuget/dt/FastMediator.svg?cacheSeconds=3600)](https://www.nuget.org/packages/FastMediator)
[![License](https://img.shields.io/github/license/DDT88/FastMediator.svg)](https://github.com/DDT88/FastMediator/blob/main/LICENSE)

---

<a id="english"></a>
# 🇬🇧 English

FastMediator is a lightweight and high-performance implementation of the Mediator pattern for .NET, optimized for performance and ease of use. Designed for applications that require high throughput with minimal overhead, it allows decoupling application components by implementing CQRS (Command Query Responsibility Segregation) in a simple and elegant way.

## Features

- 🚀 **High Performance**: Uses compiled expressions and delegate caching for optimal performance
- 🧩 **CQRS Support**: Clear separation between commands (requests that modify state) and queries (requests that return data)
- 🔄 **Behavior Pipeline**: Ability to intercept requests with configurable behaviors such as validation, logging, and performance measurement
- 📢 **Notification System**: Support for the publish/subscribe pattern with notifications to multiple handlers
- 🔍 **Integrated Diagnostics**: Detailed logging functionality and performance measurement to simplify debugging and optimization
- ✅ **Integrated Validation**: Validation of incoming requests before processing
- 🧰 **Simple Configuration**: Seamless integration with Microsoft.Extensions.DependencyInjection
- 🔄 **Full Asynchronous Support**: Fully asynchronous API and pipeline with CancellationToken support
- ⚡ **Flexible Registration Modes**: Startup, LazyLoading, or Hybrid to optimize performance and resource consumption
- 🔄 **Synchronous/Asynchronous Interoperability**: Smooth conversion between synchronous and asynchronous requests

## Installation

```bash
dotnet add package FastMediator
```

## Configuration

Configure FastMediator in your IoC container with different registration modes:

```csharp
services.AddFastMediator(scan => scan.FromAssemblyOf<Program>(), options =>
{
    // Enable optional behaviors
    options.EnableDiagnostics = true;    // Enable diagnostic behaviors
    options.EnableTiming = true;         // Enable timing measurement
    options.EnableDetailedLogging = true; // Enable detailed logging
    options.EnableValidation = true;     // Validation behavior (default: true)
    options.LoggerFactory = loggerFactory; // Optional: factory for loggers
    
    // Choose handler registration mode
    options.RegistrationMode = HandlerRegistrationMode.Startup; // All at startup (default)
    // OR
    options.RegistrationMode = HandlerRegistrationMode.LazyLoading; // On first use
    // OR
    options.UseHybridMode()               // Hybrid mode with fluent API
           .WithWarmup<PingRequest>()     // Preload specific handlers
           .WithWarmup<AnotherRequest>(); // Add more types to preload
});
```

## Basic Usage

### 1. Define a Request and its Handler

```csharp
// Synchronous request
public class Ping : IRequest<string>
{
    public string Message { get; }
    
    public Ping(string message)
    {
        Message = message;
    }
}

// Synchronous handler
public class PingHandler : IRequestHandler<Ping, string>
{
    public string Handle(Ping request)
    {
        return $"Response to: {request.Message}";
    }
}

// Asynchronous request
public class AsyncPing : IAsyncRequest<string>
{
    public string Message { get; }
    
    public AsyncPing(string message)
    {
        Message = message;
    }
}

// Asynchronous handler
public class AsyncPingHandler : IAsyncRequestHandler<AsyncPing, string>
{
    public async Task<string> HandleAsync(AsyncPing request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return $"Asynchronous response to: {request.Message}";
    }
}
```

### 2. Send the Request

```csharp
// Inject the dispatcher
public class MyService
{
    private readonly Dispatcher _mediator;
    
    public MyService(Dispatcher mediator)
    {
        _mediator = mediator;
    }
    
    // Synchronous sending
    public void ProcessMessage(string message)
    {
        string response = _mediator.Send(new Ping(message));
        Console.WriteLine(response);
    }
    
    // Asynchronous sending
    public async Task ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        string response = await _mediator.SendAsync(new AsyncPing(message), cancellationToken);
        Console.WriteLine(response);
    }
    
    // Synchronous sending using asynchronous API
    public async Task ProcessSyncMessageAsAsync(string message)
    {
        string response = await _mediator.SendAsAsync<Ping, string>(new Ping(message));
        Console.WriteLine(response);
    }
    
    // Asynchronous sending using synchronous API
    public void ProcessAsyncMessageSync(string message)
    {
        string response = _mediator.SendSync<AsyncPing, string>(new AsyncPing(message));
        Console.WriteLine(response);
    }
}
```

## Notifications

Notifications allow publishing events to multiple handlers (synchronous and asynchronous).

### 1. Define a Notification and its Handlers

```csharp
// Synchronous notification
public class SomethingHappened : INotification
{
    public string Message { get; set; }
}

// Synchronous handler
public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
{
    public void Handle(SomethingHappened notification)
    {
        Console.WriteLine($"Event handled: {notification.Message}");
    }
}

// Asynchronous notification
public class AsyncSomethingHappened : IAsyncNotification
{
    public string Message { get; set; }
}

// Asynchronous handler
public class AsyncSomethingHappenedHandler : IAsyncNotificationHandler<AsyncSomethingHappened>
{
    public async Task HandleAsync(AsyncSomethingHappened notification, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        Console.WriteLine($"Asynchronous event handled: {notification.Message}");
    }
}
```

### 2. Publish the Notification

```csharp
// Synchronous publishing
_mediator.Publish(new SomethingHappened { Message = "An important event has occurred!" });

// Asynchronous publishing (executes all handlers in parallel)
await _mediator.PublishAsync(new AsyncSomethingHappened { Message = "An asynchronous event has occurred!" });

// Sequential asynchronous publishing (one handler at a time)
await _mediator.PublishSequentialAsync(new AsyncSomethingHappened { Message = "A sequential event has occurred!" });
```

## Behavior Pipeline

Behaviors allow intercepting and manipulating requests before they reach the handler.

### Included Behaviors

FastMediator includes several ready-to-use behaviors for synchronous and asynchronous requests:

- `ValidationBehavior`/`ValidationBehaviorAsync`: Validates requests before processing
- `LoggingBehavior`/`LoggingBehaviorAsync`: Logs details of requests and responses
- `TimingBehavior`/`TimingBehaviorAsync`: Measures the processing time of requests
- `DiagnosticBehavior`/`DiagnosticBehaviorAsync`: Provides information about the behavior pipeline

### Creating a Custom Behavior

```csharp
// Synchronous behavior
public class MyCustomBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
    where TRequest : IRequest<TResponse>
{
    // Execution priority (lower = higher priority)
    public int Order => 100;
    
    public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
    {
        // Pre-processing logic
        Console.WriteLine($"Pre-processing for {typeof(TRequest).Name}");
        
        // Call the next handler in the pipeline
        var response = next(request);
        
        // Post-processing logic
        Console.WriteLine($"Post-processing for {typeof(TRequest).Name}");
        
        return response;
    }
}

// Asynchronous behavior
public class MyCustomAsyncBehavior<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
    where TRequest : IAsyncRequest<TResponse>
{
    // Execution priority (lower = higher priority)
    public int Order => 100;
    
    public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Pre-processing logic
        Console.WriteLine($"Asynchronous pre-processing for {typeof(TRequest).Name}");
        
        // Call the next handler in the pipeline
        var response = await next(request, cancellationToken);
        
        // Post-processing logic
        Console.WriteLine($"Asynchronous post-processing for {typeof(TRequest).Name}");
        
        return response;
    }
}

// Registration in the container
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MyCustomBehavior<,>));
services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(MyCustomAsyncBehavior<,>));
```

## Validation

FastMediator includes an integrated validation system, for both synchronous and asynchronous requests.

### 1. Create a Validator

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    protected override void ValidateInternal(CreateUserCommand request, ValidationResult result)
    {
        if (string.IsNullOrEmpty(request.Username))
            result.AddError(nameof(request.Username), "Username is required");
            
        if (request.Password?.Length < 8)
            result.AddError(nameof(request.Password), "Password must be at least 8 characters");
    }
}
```

### 2. Register it in the IoC Container

The validator is automatically registered if you use the assembly scanning method.

## Registration Modes

FastMediator supports different handler registration modes to optimize performance and resource usage:

```csharp
// 1. Startup Mode (default) - All handlers are registered at startup
options.RegistrationMode = HandlerRegistrationMode.Startup;

// 2. LazyLoading Mode - Handlers are registered on first use
options.RegistrationMode = HandlerRegistrationMode.LazyLoading;

// 3. Hybrid Mode - Preloads only specified handlers, others on first use
options.UseHybridMode()
       .WithWarmup<PingRequest>()
       .WithWarmup<AnotherRequest>();
```

## DelegateCache and Performance

FastMediator uses a delegate caching system to maximize performance:

```csharp
// Get cache statistics
var stats = mediator.GetRequestHandlerCacheStats();
Console.WriteLine($"Cache hits: {stats.Hits}, misses: {stats.Misses}");

// Cache size
Console.WriteLine($"Cache request handlers: {mediator.RequestHandlerCacheSize}");
Console.WriteLine($"Cache notification handlers: {mediator.NotificationHandlerCacheSize}");
Console.WriteLine($"Cache async request handlers: {mediator.AsyncRequestHandlerCacheSize}");
Console.WriteLine($"Cache async notification handlers: {mediator.AsyncNotificationHandlerCacheSize}");
```

## Synchronous/Asynchronous Interoperability

FastMediator offers extension methods to convert synchronous calls to asynchronous and vice versa:

```csharp
// From synchronous to asynchronous
await mediator.SendAsAsync<PingRequest, string>(new PingRequest("Test"));
await mediator.PublishAsAsync(new SomethingHappened { Message = "Test" });

// From asynchronous to synchronous
string result = mediator.SendSync<AsyncPingRequest, string>(new AsyncPingRequest("Test"));
mediator.PublishSync(new AsyncSomethingHappened { Message = "Test" });
```

## Advanced Scenarios

### CQRS with Different Request Types

```csharp
// Synchronous query (returns data without state modification)
public class GetUserQuery : IRequest<UserDto> { public int UserId { get; set; } }

// Synchronous command (modifies state)
public class CreateUserCommand : IRequest<int> 
{ 
    public string Username { get; set; }
    public string Email { get; set; }
}

// Asynchronous query
public class GetUserAsyncQuery : IAsyncRequest<UserDto> { public int UserId { get; set; } }

// Asynchronous command
public class CreateUserAsyncCommand : IAsyncRequest<int>
{
    public string Username { get; set; }
    public string Email { get; set; }
}
```

### Exception Handling with Behaviors

```csharp
public class ErrorHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ErrorHandlingBehavior<TRequest, TResponse>> _logger;
    
    public ErrorHandlingBehavior(ILogger<ErrorHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
    {
        try
        {
            return next(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during processing of {typeof(TRequest).Name}");
            throw; // Or handle the exception appropriately
        }
    }
}
```

## Best Practices

1. **Keep requests immutable**: Define properties as `readonly` or use C# records
2. **Use appropriate return types**: Return `void` or `Task` for commands, specific data for queries
3. **Separate requests and handlers**: Keep each handler in a separate file to improve code organization
4. **Use behaviors for cross-cutting concerns**: Validation, logging, caching, etc.
5. **Order behaviors correctly**: Use the `IOrderedPipelineBehavior` interface to control execution order
6. **Choose the appropriate registration mode**:
   - `Startup` for maximum performance in production
   - `LazyLoading` to reduce startup time and memory usage
   - `Hybrid` for an optimal compromise
7. **Prefer asynchronous APIs** for I/O-bound or potentially blocking operations

## Benchmarks vs MediatR

Head-to-head comparison against **MediatR 12.4.1** (BenchmarkDotNet, .NET 8, `ShortRunJob`).
Reproduce with:

```bash
dotnet run -c Release --project FastMediator.Benchmarks -- --filter *MediatRComparison*
```

| Scenario                     | FastMediator      | MediatR           | Result                              |
|------------------------------|-------------------|-------------------|-------------------------------------|
| Send (sync, no behavior)     | **47.7 ns / 96 B**  | 76.6 ns / 240 B  | ~1.6× faster, ~2.5× less memory     |
| Send (async, no behavior)    | **72.8 ns / 168 B** | 76.6 ns / 240 B  | faster, ~1.4× less memory           |
| Send (async, 1 behavior)     | **93.3 ns / 384 B** | 103.4 ns / 432 B | ~1.1× faster, less memory           |
| Publish (notification)       | **40.4 ns / 88 B**  | 75.3 ns / 288 B  | ~1.9× faster, ~3.3× less memory     |

> Numbers vary by hardware/runtime; run the benchmark on your machine for exact figures.
> MediatR is async-only, so the sync `Send` row has no MediatR counterpart and is shown
> against MediatR's async `Send` for reference.

## Contributing

Contributions are welcome! If you want to improve FastMediator, feel free to send a pull request.

## License

FastMediator is distributed under the MIT license. See the LICENSE file for more details.

---

<a id="italiano"></a>
# 🇮🇹 Italiano

FastMediator è un'implementazione leggera e ad alte prestazioni del pattern Mediator per .NET, ottimizzata per le prestazioni e la facilità d'uso. Progettata per applicazioni che richiedono un throughput elevato con un overhead minimo, consente di disaccoppiare i componenti dell'applicazione implementando la CQRS (Command Query Responsibility Segregation) in modo semplice ed elegante.

## Caratteristiche

- 🚀 **Alte Prestazioni**: Utilizza espressioni compilate e caching dei delegati per prestazioni ottimali
- 🧩 **Supporto CQRS**: Chiara separazione tra comandi (richieste che modificano lo stato) e query (richieste che restituiscono dati)
- 🔄 **Pipeline dei Comportamenti**: Possibilità di intercettare le richieste con comportamenti configurabili come validazione, logging e misurazione delle prestazioni
- 📢 **Sistema di Notifica**: Supporto per il pattern publish/subscribe con notifiche a più gestori (handlers)
- 🔍 **Diagnostica Integrata**: Funzionalità dettagliate di logging e misurazione delle prestazioni per semplificare il debug e l'ottimizzazione
- ✅ **Validazione Integrata**: Validazione delle richieste in ingresso prima dell'elaborazione
- 🧰 **Configurazione Semplice**: Integrazione perfetta con Microsoft.Extensions.DependencyInjection
- 🔄 **Supporto Asincrono Completo**: API e pipeline completamente asincrone con supporto per CancellationToken
- ⚡ **Modalità di Registrazione Flessibili**: Startup, LazyLoading o Ibrida per ottimizzare le prestazioni e il consumo di risorse
- 🔄 **Interoperabilità Sincrona/Asincrona**: Conversione agevole tra richieste sincrone e asincrone

## Installazione

```bash
dotnet add package FastMediator
```

## Configurazione

Configura FastMediator nel tuo container IoC con diverse modalità di registrazione:

```csharp
services.AddFastMediator(scan => scan.FromAssemblyOf<Program>(), options =>
{
    // Abilita comportamenti opzionali
    options.EnableDiagnostics = true;    // Abilita comportamenti diagnostici
    options.EnableTiming = true;         // Abilita la misurazione dei tempi
    options.EnableDetailedLogging = true; // Abilita il logging dettagliato
    options.EnableValidation = true;     // Comportamento di validazione (predefinito: true)
    options.LoggerFactory = loggerFactory; // Opzionale: factory per i logger
    
    // Scegli la modalità di registrazione dei gestori
    options.RegistrationMode = HandlerRegistrationMode.Startup; // Tutti all'avvio (predefinito)
    // OPPURE
    options.RegistrationMode = HandlerRegistrationMode.LazyLoading; // Al primo utilizzo
    // OPPURE
    options.UseHybridMode()               // Modalità ibrida con API fluente
           .WithWarmup<PingRequest>()     // Precarica gestori specifici
           .WithWarmup<AnotherRequest>(); // Aggiungi altri tipi da precaricare
});
```

## Utilizzo Base

### 1. Definire una Richiesta e il suo Gestore (Handler)

```csharp
// Richiesta sincrona
public class Ping : IRequest<string>
{
    public string Message { get; }
    
    public Ping(string message)
    {
        Message = message;
    }
}

// Gestore sincrono
public class PingHandler : IRequestHandler<Ping, string>
{
    public string Handle(Ping request)
    {
        return $"Risposta a: {request.Message}";
    }
}

// Richiesta asincrona
public class AsyncPing : IAsyncRequest<string>
{
    public string Message { get; }
    
    public AsyncPing(string message)
    {
        Message = message;
    }
}

// Gestore asincrono
public class AsyncPingHandler : IAsyncRequestHandler<AsyncPing, string>
{
    public async Task<string> HandleAsync(AsyncPing request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return $"Risposta asincrona a: {request.Message}";
    }
}
```

### 2. Inviare la Richiesta

```csharp
// Iniettare il dispatcher
public class MyService
{
    private readonly Dispatcher _mediator;
    
    public MyService(Dispatcher mediator)
    {
        _mediator = mediator;
    }
    
    // Invio sincrono
    public void ProcessMessage(string message)
    {
        string response = _mediator.Send(new Ping(message));
        Console.WriteLine(response);
    }
    
    // Invio asincrono
    public async Task ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        string response = await _mediator.SendAsync(new AsyncPing(message), cancellationToken);
        Console.WriteLine(response);
    }
    
    // Invio sincrono tramite API asincrona
    public async Task ProcessSyncMessageAsAsync(string message)
    {
        string response = await _mediator.SendAsAsync<Ping, string>(new Ping(message));
        Console.WriteLine(response);
    }
    
    // Invio asincrono tramite API sincrona
    public void ProcessAsyncMessageSync(string message)
    {
        string response = _mediator.SendSync<AsyncPing, string>(new AsyncPing(message));
        Console.WriteLine(response);
    }
}
```

## Notifiche

Le notifiche consentono di pubblicare eventi su più gestori (sincroni e asincroni).

### 1. Definire una Notifica e i suoi Gestori

```csharp
// Notifica sincrona
public class SomethingHappened : INotification
{
    public string Message { get; set; }
}

// Gestore sincrono
public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
{
    public void Handle(SomethingHappened notification)
    {
        Console.WriteLine($"Evento gestito: {notification.Message}");
    }
}

// Notifica asincrona
public class AsyncSomethingHappened : IAsyncNotification
{
    public string Message { get; set; }
}

// Gestore asincrono
public class AsyncSomethingHappenedHandler : IAsyncNotificationHandler<AsyncSomethingHappened>
{
    public async Task HandleAsync(AsyncSomethingHappened notification, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        Console.WriteLine($"Evento asincrono gestito: {notification.Message}");
    }
}
```

### 2. Pubblicare la Notifica

```csharp
// Pubblicazione sincrona
_mediator.Publish(new SomethingHappened { Message = "Si è verificato un evento importante!" });

// Pubblicazione asincrona (esegue tutti i gestori in parallelo)
await _mediator.PublishAsync(new AsyncSomethingHappened { Message = "Si è verificato un evento asincrono!" });

// Pubblicazione asincrona sequenziale (un gestore alla volta)
await _mediator.PublishSequentialAsync(new AsyncSomethingHappened { Message = "Si è verificato un evento sequenziale!" });
```

## Pipeline dei Comportamenti

I comportamenti consentono di intercettare e manipolare le richieste prima che raggiungano il gestore.

### Comportamenti Inclusi

FastMediator include vari comportamenti pronti all'uso per richieste sincrone e asincrone:

- `ValidationBehavior`/`ValidationBehaviorAsync`: Valida le richieste prima dell'elaborazione
- `LoggingBehavior`/`LoggingBehaviorAsync`: Registra i dettagli di richieste e risposte
- `TimingBehavior`/`TimingBehaviorAsync`: Misura il tempo di elaborazione delle richieste
- `DiagnosticBehavior`/`DiagnosticBehaviorAsync`: Fornisce informazioni sulla pipeline dei comportamenti

### Creare un Comportamento Personalizzato

```csharp
// Comportamento sincrono
public class MyCustomBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
    where TRequest : IRequest<TResponse>
{
    // Priorità di esecuzione (minore = priorità maggiore)
    public int Order => 100;
    
    public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
    {
        // Logica di pre-elaborazione
        Console.WriteLine($"Pre-elaborazione per {typeof(TRequest).Name}");
        
        // Chiama il gestore successivo nella pipeline
        var response = next(request);
        
        // Logica di post-elaborazione
        Console.WriteLine($"Post-elaborazione per {typeof(TRequest).Name}");
        
        return response;
    }
}

// Comportamento asincrono
public class MyCustomAsyncBehavior<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
    where TRequest : IAsyncRequest<TResponse>
{
    // Priorità di esecuzione (minore = priorità maggiore)
    public int Order => 100;
    
    public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Logica di pre-elaborazione
        Console.WriteLine($"Pre-elaborazione asincrona per {typeof(TRequest).Name}");
        
        // Chiama il gestore successivo nella pipeline
        var response = await next(request, cancellationToken);
        
        // Logica di post-elaborazione
        Console.WriteLine($"Post-elaborazione asincrona per {typeof(TRequest).Name}");
        
        return response;
    }
}

// Registrazione nel container
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MyCustomBehavior<,>));
services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(MyCustomAsyncBehavior<,>));
```

## Validazione

FastMediator include un sistema di validazione integrato, sia per richieste sincrone che asincrone.

### 1. Creare un Validatore

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    protected override void ValidateInternal(CreateUserCommand request, ValidationResult result)
    {
        if (string.IsNullOrEmpty(request.Username))
            result.AddError(nameof(request.Username), "L'username è obbligatorio");
            
        if (request.Password?.Length < 8)
            result.AddError(nameof(request.Password), "La password deve avere almeno 8 caratteri");
    }
}
```

### 2. Registrarlo nel Container IoC

Il validatore viene registrato automaticamente se si utilizza il metodo di scansione dell'assembly.

## Modalità di Registrazione

FastMediator supporta diverse modalità di registrazione dei gestori per ottimizzare le prestazioni e l'uso delle risorse:

```csharp
// 1. Modalità Startup (predefinita) - Tutti i gestori vengono registrati all'avvio
options.RegistrationMode = HandlerRegistrationMode.Startup;

// 2. Modalità LazyLoading - I gestori vengono registrati al primo utilizzo
options.RegistrationMode = HandlerRegistrationMode.LazyLoading;

// 3. Modalità Ibrida - Precarica solo i gestori specificati, gli altri al primo utilizzo
options.UseHybridMode()
       .WithWarmup<PingRequest>()
       .WithWarmup<AnotherRequest>();
```

## DelegateCache e Prestazioni

FastMediator utilizza un sistema di caching dei delegati per massimizzare le prestazioni:

```csharp
// Ottieni statistiche della cache
var stats = mediator.GetRequestHandlerCacheStats();
Console.WriteLine($"Hit della cache: {stats.Hits}, miss: {stats.Misses}");

// Dimensione della cache
Console.WriteLine($"Cache gestori richieste: {mediator.RequestHandlerCacheSize}");
Console.WriteLine($"Cache gestori notifiche: {mediator.NotificationHandlerCacheSize}");
Console.WriteLine($"Cache gestori richieste asincrone: {mediator.AsyncRequestHandlerCacheSize}");
Console.WriteLine($"Cache gestori notifiche asincrone: {mediator.AsyncNotificationHandlerCacheSize}");
```

## Interoperabilità Sincrona/Asincrona

FastMediator offre metodi di estensione per convertire chiamate sincrone in asincrone e viceversa:

```csharp
// Da sincrono ad asincrono
await mediator.SendAsAsync<PingRequest, string>(new PingRequest("Test"));
await mediator.PublishAsAsync(new SomethingHappened { Message = "Test" });

// Da asincrono a sincrono
string result = mediator.SendSync<AsyncPingRequest, string>(new AsyncPingRequest("Test"));
mediator.PublishSync(new AsyncSomethingHappened { Message = "Test" });
```

## Scenari Avanzati

### CQRS con Diversi Tipi di Richiesta

```csharp
// Query sincrona (restituisce dati senza modificare lo stato)
public class GetUserQuery : IRequest<UserDto> { public int UserId { get; set; } }

// Comando sincrono (modifica lo stato)
public class CreateUserCommand : IRequest<int> 
{ 
    public string Username { get; set; }
    public string Email { get; set; }
}

// Query asincrona
public class GetUserAsyncQuery : IAsyncRequest<UserDto> { public int UserId { get; set; } }

// Comando asincrono
public class CreateUserAsyncCommand : IAsyncRequest<int>
{
    public string Username { get; set; }
    public string Email { get; set; }
}
```

### Gestione delle Eccezioni con i Comportamenti

```csharp
public class ErrorHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ErrorHandlingBehavior<TRequest, TResponse>> _logger;
    
    public ErrorHandlingBehavior(ILogger<ErrorHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
    {
        try
        {
            return next(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Errore durante l'elaborazione di {typeof(TRequest).Name}");
            throw; // O gestisci l'eccezione appropriatamente
        }
    }
}
```

## Best Practices

1. **Mantieni le richieste immutabili**: Definisci le proprietà come `readonly` o usa i record in C#
2. **Usa i tipi di ritorno appropriati**: Restituisci `void` o `Task` per i comandi, dati specifici per le query
3. **Separa le richieste e i gestori**: Tieni ogni gestore in un file separato per migliorare l'organizzazione del codice
4. **Usa i comportamenti per aspetti trasversali**: Validazione, logging, caching, ecc.
5. **Ordina i comportamenti correttamente**: Usa l'interfaccia `IOrderedPipelineBehavior` per controllare l'ordine di esecuzione
6. **Scegli la modalità di registrazione appropriata**:
   - `Startup` per le massime prestazioni in produzione
   - `LazyLoading` per ridurre i tempi di avvio e l'utilizzo della memoria
   - `Hybrid` per un compromesso ottimale
7. **Preferisci le API asincrone** per le operazioni di I/O o potenzialmente bloccanti

## Benchmark vs MediatR

Confronto diretto con **MediatR 12.4.1** (BenchmarkDotNet, .NET 8, `ShortRunJob`).
Riproduci con:

```bash
dotnet run -c Release --project FastMediator.Benchmarks -- --filter *MediatRComparison*
```

| Scenario                     | FastMediator      | MediatR           | Risultato                           |
|------------------------------|-------------------|-------------------|-------------------------------------|
| Send (sync, no behavior)     | **47.7 ns / 96 B**  | 76.6 ns / 240 B  | ~1.6× più veloce, ~2.5× meno mem    |
| Send (async, no behavior)    | **72.8 ns / 168 B** | 76.6 ns / 240 B  | più veloce, ~1.4× meno mem          |
| Send (async, 1 behavior)     | **93.3 ns / 384 B** | 103.4 ns / 432 B | ~1.1× più veloce, meno mem          |
| Publish (notification)       | **40.4 ns / 88 B**  | 75.3 ns / 288 B  | ~1.9× più veloce, ~3.3× meno mem    |

> I numeri variano in base all'hardware/runtime; esegui il benchmark sulla tua macchina per le cifre esatte.
> MediatR è solo asincrono, quindi la riga del `Send` sincrono non ha una controparte in MediatR e viene mostrata
> rispetto al `Send` asincrono di MediatR per riferimento.

## Contribuire

I contributi sono ben accetti! Se vuoi migliorare FastMediator, sentiti libero di inviare una pull request.

## Licenza

FastMediator è distribuito sotto licenza MIT. Vedi il file LICENSE per maggiori dettagli.