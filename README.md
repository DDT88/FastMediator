# FastMediator

[![NuGet](https://img.shields.io/nuget/v/FastMediator.svg)](https://www.nuget.org/packages/FastMediator)
[![NuGet](https://img.shields.io/nuget/dt/FastMediator.svg?cacheSeconds=3600)](https://www.nuget.org/packages/FastMediator)
[![License](https://img.shields.io/github/license/DDT88/FastMediator.svg)](https://github.com/DDT88/FastMediator/blob/main/LICENSE)

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
services.AddCustomMediator(scan => scan.FromAssemblyOf<Program>(), options =>
{
    // Enable optional behaviors
    options.EnableDiagnostics = true;    // Enable diagnostic behaviors
    options.EnableTiming = true;         // Enable timing measurement
    options.EnableDetailedLogging = true; // Enable detailed logging
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

## Contributing

Contributions are welcome! If you want to improve FastMediator, feel free to send a pull request.

## License

FastMediator is distributed under the MIT license. See the LICENSE file for more details.