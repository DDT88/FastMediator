# FastMediator

[![NuGet](https://img.shields.io/nuget/v/FastMediator.svg)](https://www.nuget.org/packages/FastMediator)
[![NuGet](https://img.shields.io/nuget/dt/FastMediator.svg?cacheSeconds=3600)](https://www.nuget.org/packages/FastMediator)
[![License](https://img.shields.io/github/license/DDT88/FastMediator.svg)](https://github.com/DDT88/FastMediator/blob/main/LICENSE)

FastMediator è un'implementazione leggera e performante del pattern Mediator per .NET, ottimizzata per prestazioni e facilità d'uso. Progettata per applicazioni che necessitano di un elevato throughput con un overhead minimo, permette di disaccoppiare i componenti dell'applicazione implementando il CQRS (Command Query Responsibility Segregation) in modo semplice ed elegante.

## Caratteristiche

- 🚀 **Alte prestazioni**: Utilizza espressioni compilate e caching dei delegati per ottenere prestazioni ottimali
- 🧩 **Supporto CQRS**: Separazione netta tra comandi (richieste che modificano lo stato) e query (richieste che restituiscono dati)
- 🔄 **Pipeline di comportamenti**: Possibilità di intercettare richieste con comportamenti configurabili come validazione, logging e misurazione delle prestazioni
- 📢 **Sistema di notifiche**: Supporto per il pattern publish/subscribe con notifiche a più handler
- 🔍 **Diagnostica integrata**: Funzionalità di logging dettagliato e misurazione delle prestazioni per semplificare il debug e l'ottimizzazione
- ✅ **Validazione integrata**: Validazione delle richieste in ingresso prima dell'elaborazione
- 🧰 **Configurazione semplice**: Integrazione perfetta con Microsoft.Extensions.DependencyInjection
- 🔄 **Supporto asincrono completo**: API e pipeline completamente asincrone con supporto per CancellationToken
- ⚡ **Modalità di registrazione flessibili**: Startup, LazyLoading o Hybrid per ottimizzare performance e consumo di risorse
- 🔄 **Interoperabilità sincrona/asincrona**: Conversione fluida tra richieste sincrone e asincrone

## Installazione

```bash
dotnet add package FastMediator
```

## Configurazione

Configura FastMediator nel tuo container IoC con diverse modalità di registrazione:

```csharp
services.AddCustomMediator(scan => scan.FromAssemblyOf<Program>(), options =>
{
    // Abilita comportamenti opzionali
    options.EnableDiagnostics = true;    // Abilita behavior diagnostici
    options.EnableTiming = true;         // Abilita misurazione tempi
    options.EnableDetailedLogging = true; // Abilita logging dettagliato
    options.LoggerFactory = loggerFactory; // Facoltativo: factory per i logger
    
    // Scegli la modalità di registrazione degli handler
    options.RegistrationMode = HandlerRegistrationMode.Startup; // Tutti all'avvio (default)
    // OPPURE
    options.RegistrationMode = HandlerRegistrationMode.LazyLoading; // Al primo utilizzo
    // OPPURE
    options.UseHybridMode()               // Modalità ibrida con API fluent
           .WithWarmup<PingRequest>()     // Precarica handler specifici
           .WithWarmup<AnotherRequest>(); // Aggiungi altri tipi da precaricare
});
```

## Utilizzo Base

### 1. Definisci una Richiesta e il suo Handler

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

// Handler sincrono
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

// Handler asincrono
public class AsyncPingHandler : IAsyncRequestHandler<AsyncPing, string>
{
    public async Task<string> HandleAsync(AsyncPing request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return $"Risposta asincrona a: {request.Message}";
    }
}
```

### 2. Invia la Richiesta

```csharp
// Inietta il dispatcher
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
    
    // Invio sincrono usando API asincrona
    public async Task ProcessSyncMessageAsAsync(string message)
    {
        string response = await _mediator.SendAsAsync<Ping, string>(new Ping(message));
        Console.WriteLine(response);
    }
    
    // Invio asincrono usando API sincrona
    public void ProcessAsyncMessageSync(string message)
    {
        string response = _mediator.SendSync<AsyncPing, string>(new AsyncPing(message));
        Console.WriteLine(response);
    }
}
```

## Notifiche

Le notifiche permettono la pubblicazione di eventi a più handler (sincrone e asincrone).

### 1. Definisci una Notifica e i suoi Handler

```csharp
// Notifica sincrona
public class SomethingHappened : INotification
{
    public string Message { get; set; }
}

// Handler sincrono
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

// Handler asincrono
public class AsyncSomethingHappenedHandler : IAsyncNotificationHandler<AsyncSomethingHappened>
{
    public async Task HandleAsync(AsyncSomethingHappened notification, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        Console.WriteLine($"Evento asincrono gestito: {notification.Message}");
    }
}
```

### 2. Pubblica la Notifica

```csharp
// Pubblicazione sincrona
_mediator.Publish(new SomethingHappened { Message = "Un evento importante è accaduto!" });

// Pubblicazione asincrona (esegue tutti gli handler in parallelo)
await _mediator.PublishAsync(new AsyncSomethingHappened { Message = "Un evento asincrono è accaduto!" });

// Pubblicazione asincrona sequenziale (un handler alla volta)
await _mediator.PublishSequentialAsync(new AsyncSomethingHappened { Message = "Un evento sequenziale è accaduto!" });
```

## Pipeline di Behaviors

I behaviors permettono di intercettare e manipolare le richieste prima che raggiungano l'handler.

### Behavior Inclusi

FastMediator include diversi behaviors pronti all'uso per richieste sincrone e asincrone:

- `ValidationBehavior`/`ValidationBehaviorAsync`: Valida le richieste prima dell'elaborazione
- `LoggingBehavior`/`LoggingBehaviorAsync`: Registra dettagli delle richieste e delle risposte
- `TimingBehavior`/`TimingBehaviorAsync`: Misura il tempo di elaborazione delle richieste
- `DiagnosticBehavior`/`DiagnosticBehaviorAsync`: Fornisce informazioni sulla pipeline di behaviors

### Creazione di un Behavior Personalizzato

```csharp
// Behavior sincrono
public class MyCustomBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
    where TRequest : IRequest<TResponse>
{
    // Priorità di esecuzione (più basso = più alta priorità)
    public int Order => 100;
    
    public TResponse Handle(TRequest request, Func<TRequest, TResponse> next)
    {
        // Logica pre-elaborazione
        Console.WriteLine($"Pre-elaborazione per {typeof(TRequest).Name}");
        
        // Chiama il prossimo handler nella pipeline
        var response = next(request);
        
        // Logica post-elaborazione
        Console.WriteLine($"Post-elaborazione per {typeof(TRequest).Name}");
        
        return response;
    }
}

// Behavior asincrono
public class MyCustomAsyncBehavior<TRequest, TResponse> : IPipelineBehaviorAsync<TRequest, TResponse>, IOrderedPipelineBehavior
    where TRequest : IAsyncRequest<TResponse>
{
    // Priorità di esecuzione (più basso = più alta priorità)
    public int Order => 100;
    
    public async Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        // Logica pre-elaborazione
        Console.WriteLine($"Pre-elaborazione asincrona per {typeof(TRequest).Name}");
        
        // Chiama il prossimo handler nella pipeline
        var response = await next(request, cancellationToken);
        
        // Logica post-elaborazione
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

### 1. Crea un Validatore

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    protected override void ValidateInternal(CreateUserCommand request, ValidationResult result)
    {
        if (string.IsNullOrEmpty(request.Username))
            result.AddError(nameof(request.Username), "Username è obbligatorio");
            
        if (request.Password?.Length < 8)
            result.AddError(nameof(request.Password), "La password deve contenere almeno 8 caratteri");
    }
}
```

### 2. Registralo nel Container IoC

Il validatore viene registrato automaticamente se utilizzi il metodo di scansione dell'assembly.

## Modalità di Registrazione

FastMediator supporta diverse modalità di registrazione degli handler per ottimizzare prestazioni e utilizzo di risorse:

```csharp
// 1. Modalità Startup (default) - Tutti gli handler vengono registrati all'avvio
options.RegistrationMode = HandlerRegistrationMode.Startup;

// 2. Modalità LazyLoading - Gli handler vengono registrati al primo utilizzo
options.RegistrationMode = HandlerRegistrationMode.LazyLoading;

// 3. Modalità Hybrid - Precarica solo gli handler specificati, gli altri al primo utilizzo
options.UseHybridMode()
       .WithWarmup<PingRequest>()
       .WithWarmup<AnotherRequest>();
```

## DelegateCache e Prestazioni

FastMediator utilizza un sistema di cache dei delegati per massimizzare le prestazioni:

```csharp
// Ottieni statistiche sulla cache
var stats = mediator.GetRequestHandlerCacheStats();
Console.WriteLine($"Cache hits: {stats.Hits}, misses: {stats.Misses}");

// Dimensione della cache
Console.WriteLine($"Cache request handlers: {mediator.RequestHandlerCacheSize}");
Console.WriteLine($"Cache notification handlers: {mediator.NotificationHandlerCacheSize}");
Console.WriteLine($"Cache async request handlers: {mediator.AsyncRequestHandlerCacheSize}");
Console.WriteLine($"Cache async notification handlers: {mediator.AsyncNotificationHandlerCacheSize}");
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

### CQRS con Differenti Tipi di Richieste

```csharp
// Query sincrona (restituzione dati senza modifiche di stato)
public class GetUserQuery : IRequest<UserDto> { public int UserId { get; set; } }

// Command sincrono (modifica lo stato)
public class CreateUserCommand : IRequest<int> 
{ 
    public string Username { get; set; }
    public string Email { get; set; }
}

// Query asincrona
public class GetUserAsyncQuery : IAsyncRequest<UserDto> { public int UserId { get; set; } }

// Command asincrono
public class CreateUserAsyncCommand : IAsyncRequest<int>
{
    public string Username { get; set; }
    public string Email { get; set; }
}
```

### Gestione delle Eccezioni con Behaviors

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
            throw; // O gestisci l'eccezione in modo appropriato
        }
    }
}
```

## Best Practices

1. **Tenere le richieste immutabili**: Definisci le proprietà come `readonly` o utilizza record C#
2. **Usare tipi di ritorno appropriati**: Ritorna `void` o `Task` per comandi, dati specifici per query
3. **Separare richieste e handler**: Mantieni ciascun handler in un file separato per migliorare l'organizzazione del codice
4. **Utilizzare behavior per preoccupazioni trasversali**: Validazione, logging, caching, ecc.
5. **Ordinare i behaviors correttamente**: Usa l'interfaccia `IOrderedPipelineBehavior` per controllare l'ordine di esecuzione
6. **Scegliere la modalità di registrazione appropriata**:
   - `Startup` per massime prestazioni in produzione
   - `LazyLoading` per ridurre il tempo di avvio e l'utilizzo di memoria
   - `Hybrid` per un compromesso ottimale
7. **Preferire API asincrone** per operazioni I/O-bound o potenzialmente bloccanti

## Contribuire

Le contribuzioni sono benvenute! Se desideri migliorare FastMediator, sentiti libero di inviare una pull request.

## Licenza

FastMediator è distribuito sotto la licenza MIT. Vedi il file LICENSE per maggiori dettagli.
