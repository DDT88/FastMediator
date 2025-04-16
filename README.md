# FastMediator

[![NuGet](https://img.shields.io/nuget/v/FastMediator.svg)](https://www.nuget.org/packages/FastMediator)
[![NuGet](https://img.shields.io/nuget/dt/FastMediator.svg)](https://www.nuget.org/packages/FastMediator)
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

## Installazione

```bash
dotnet add package FastMediator
```

## Configurazione

Configura FastMediator nel tuo container IoC:

```csharp
services.AddCustomMediator(scan => scan.FromAssemblyOf<Program>(), options =>
{
    options.EnableDiagnostics = true;    // Abilita behavior diagnostici
    options.EnableTiming = true;         // Abilita misurazione tempi
    options.EnableDetailedLogging = true; // Abilita logging dettagliato
    options.LoggerFactory = loggerFactory; // Facoltativo: factory per i logger
});
```

## Utilizzo Base

### 1. Definisci una Richiesta e il suo Handler

```csharp
// Richiesta
public class Ping : IRequest<string>
{
    public string Message { get; }
    
    public Ping(string message)
    {
        Message = message;
    }
}

// Handler
public class PingHandler : IRequestHandler<Ping, string>
{
    public string Handle(Ping request)
    {
        return $"Risposta a: {request.Message}";
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
    
    public void ProcessMessage(string message)
    {
        // Invia la richiesta e ottieni la risposta
        string response = _mediator.Send(new Ping(message));
        Console.WriteLine(response);
    }
}
```

## Notifiche

Le notifiche permettono la pubblicazione di eventi a più handler.

### 1. Definisci una Notifica e i suoi Handler

```csharp
// Notifica
public class SomethingHappened : INotification
{
    public string Message { get; set; }
}

// Handler
public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
{
    public void Handle(SomethingHappened notification)
    {
        Console.WriteLine($"Evento gestito: {notification.Message}");
    }
}

// Puoi avere più handler per la stessa notifica
public class AnotherSomethingHappenedHandler : INotificationHandler<SomethingHappened>
{
    public void Handle(SomethingHappened notification)
    {
        // Fai qualcos'altro con la notifica
    }
}
```

### 2. Pubblica la Notifica

```csharp
_mediator.Publish(new SomethingHappened { Message = "Un evento importante è accaduto!" });
```

## Pipeline di Behaviors

I behaviors permettono di intercettare e manipolare le richieste prima che raggiungano l'handler.

### Behavior Inclusi

FastMediator include diversi behaviors pronti all'uso:

- `ValidationBehavior`: Valida le richieste prima dell'elaborazione
- `LoggingBehavior`: Registra dettagli delle richieste e delle risposte
- `TimingBehavior`: Misura il tempo di elaborazione delle richieste
- `DiagnosticBehavior`: Fornisce informazioni sulla pipeline di behaviors

### Creazione di un Behavior Personalizzato

```csharp
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

// Registralo nel container
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MyCustomBehavior<,>));
```

## Validazione

FastMediator include un sistema di validazione integrato.

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

## Esempi Avanzati

### CQRS con Differenti Tipo di Richieste

```csharp
// Query (restituzione dati senza modifiche di stato)
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

// Command (modifica lo stato)
public class CreateUserCommand : IRequest<int>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}
```

### Logging e Diagnostica

```csharp
var stats = mediator.GetRequestHandlerCacheStats();
Console.WriteLine($"Cache hits: {stats.Hits}, misses: {stats.Misses}");
Console.WriteLine($"Cache size: {mediator.RequestHandlerCacheSize}");
```

## Performance

FastMediator è progettato per alte prestazioni:

- Utilizza espressioni compilate per generare delegati fortemente tipizzati
- Memorizza nella cache i delegati generati per minimizzare l'overhead di reflection
- Implementa una pipeline di comportamenti ottimizzata
- Supporta l'ordinamento esplicito dei behaviors per un controllo preciso del flusso di esecuzione

## Best Practices

1. **Tenere le richieste immutabili**: Definisci le proprietà come `readonly` o utilizza record C#
2. **Usare tipi di ritorno appropriati**: Ritorna `void` o `Task` per comandi, dati specifici per query
3. **Separare richieste e handler**: Mantieni ciascun handler in un file separato per migliorare l'organizzazione del codice
4. **Utilizzare behavior per preoccupazioni trasversali**: Validazione, logging, caching, ecc.
5. **Ordinare i behaviors correttamente**: Usa l'interfaccia `IOrderedPipelineBehavior` per controllare l'ordine di esecuzione

## Contribuire

Le contribuzioni sono benvenute! Se desideri migliorare FastMediator, sentiti libero di inviare una pull request.

## Licenza

FastMediator è distribuito sotto la licenza MIT. Vedi il file LICENSE per maggiori dettagli.
