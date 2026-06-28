# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FastMediator is a high-performance .NET 8 library implementing the Mediator pattern, published as a NuGet package. It prioritizes zero-allocation hot paths by compiling handler delegates at registration time rather than using reflection per dispatch.

## Solution Structure

- **`FastMediator/`** — Core library (the NuGet package)
- **`FastMediator.UnitTests/`** — xUnit tests with FluentAssertions and Moq
- **`FastMediator.Benchmarks/`** — BenchmarkDotNet benchmarks
- **`TestConsole/`** — Manual integration/demo console app

## Common Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~Send_WithValidRequest_ReturnsCorrectResponse"

# Run benchmarks (BenchmarkSwitcher; must use Release config)
dotnet run -c Release --project FastMediator.Benchmarks -- --filter *
# MediatR head-to-head comparison only:
dotnet run -c Release --project FastMediator.Benchmarks -- --filter *MediatRComparison*

# Pack NuGet (version defined in FastMediator.csproj)
dotnet pack ./FastMediator/FastMediator.csproj -c Release
```

Publishing to NuGet is automated via GitHub Actions and triggered by pushing a `v*` tag.

## Core Architecture

### Dispatch flow

`Dispatcher` is the library's sole entry point — consumers inject it directly, not an `IMediator` interface.

- **`Dispatcher` is registered as Scoped** and resolves handlers/behaviors from the **ambient** `IServiceProvider` (the current scope). It does **not** create a per-request DI scope, so in a web request the handler's scoped dependencies share the request scope (MediatR-like semantics). Verified by `AmbientScopeTests`.
- **`DispatcherRegistry` (singleton)** owns the four pre-compiled delegate maps so compilation happens once, shared across all scoped `Dispatcher` instances:
  - `handlerMap` — `Type → Func<IServiceProvider, object, object>` for sync requests
  - `asyncHandlerMap` — `Type → Func<IServiceProvider, object, CancellationToken, Task<object>>` for async requests
  - `notificationMap` / `asyncNotificationMap` — one-to-many for notifications

Delegates are created by factory types (`RequestHandlerFactory<TReq,TRes>`, `AsyncRequestHandlerFactory<,>`, etc.) via reflected `CreateHandler` static methods at registration time. At dispatch time it's a dictionary lookup + delegate invocation — no reflection. The factory delegates resolve the handler from the ambient provider, take a **fast path** (direct handler call) when no behaviors are registered, and build the behavior pipeline with a cached `Comparison` (no per-call LINQ).

### Registration modes (`HandlerRegistrationMode`)

| Mode | Behavior |
|------|----------|
| `Startup` (default) | All handler delegates compiled when `Dispatcher` is first resolved |
| `LazyLoading` | Delegates compiled on first use, cached in `DelegateCache` (singleton) |
| `Hybrid` | Startup-compile the types in `WarmupTypes`, lazy for the rest |

### Pipeline behaviors

Behaviors implement `IPipelineBehavior<TReq,TRes>` (sync) or `IPipelineBehaviorAsync<TReq,TRes>`. Implement `IOrderedPipelineBehavior` to control execution order (lower `Order` = runs earlier). Built-in orders:

- `ValidationBehavior`: `Order = -10` (always first)
- `DiagnosticBehavior`: `Order = 999` (always last)

Behaviors are registered by `AddCustomMediator` based on `FastMediatorOptions` flags:

- `EnableDiagnostics` → `DiagnosticBehavior` + `DiagnosticBehaviorAsync`
- `EnableTiming` → `TimingBehavior` + `TimingBehaviorAsync`
- `EnableDetailedLogging` → `LoggingBehavior` + `LoggingBehaviorAsync`

`ValidationBehavior` is registered when `options.EnableValidation` is `true` (the default). Set it to `false` to remove validation from the pipeline entirely so behavior-free requests hit the fast path.

### Validation

Implement `AbstractValidator<T>` and override `ValidateInternal`. Validators are discovered by Scrutor scanning (same assembly scan as handlers) and injected into `ValidationBehavior` via `IValidator<T>`. Failures throw `ValidationException` containing a list of `ValidationError`.

### Key interfaces

| Interface | Purpose |
|-----------|---------|
| `IRequest<TResponse>` | Sync request |
| `IAsyncRequest<TResponse>` | Async request |
| `INotification` | Sync fire-and-forget notification |
| `IAsyncNotification` | Async notification |
| `IRequestHandler<TReq,TRes>` | Sync handler |
| `IAsyncRequestHandler<TReq,TRes>` | Async handler |
| `INotificationHandler<T>` / `IAsyncNotificationHandler<T>` | Notification handlers (multiple per notification) |

### DI registration

```csharp
services.AddFastMediator(options =>
{
    options.EnableTiming = true;
    options.RegistrationMode = HandlerRegistrationMode.Startup;
});
```

`AddFastMediator` is the entry point; `AddCustomMediator` remains as an `[Obsolete]` alias. Handlers, validators, and behaviors are discovered by Scrutor's `FromApplicationDependencies()` scan by default, or constrained with `scan => scan.FromAssemblyOf<T>()`. Note: Scrutor's scan registers concrete handlers but **not** open-generic pipeline behaviors — those must be registered explicitly (e.g. `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MyBehavior<,>))`).
