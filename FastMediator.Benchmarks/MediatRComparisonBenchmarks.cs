using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using FastMediator.Configuration;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using FM = FastMediator.Interfaces;

namespace FastMediator.Benchmarks.Comparison
{
    /// <summary>
    /// Confronto diretto tra FastMediator e MediatR (12.4.1) su request/response e notifiche,
    /// con e senza un behavior no-op nella pipeline.
    /// MediatR è solo asincrono, quindi il confronto principale è su SendAsync/Publish.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [RankColumn]
    [ShortRunJob]
    public class MediatRComparisonBenchmarks
    {
        private Dispatcher _fast = null!;
        private Dispatcher _fastWithBehavior = null!;
        private MediatR.IMediator _mediatr = null!;
        private MediatR.IMediator _mediatrWithBehavior = null!;

        private readonly FmRequest _fmSync = new() { Value = 1 };
        private readonly FmAsyncRequest _fmAsync = new() { Value = 1 };
        private readonly FmNotification _fmNotification = new();

        private readonly MrRequest _mrRequest = new() { Value = 1 };
        private readonly MrNotification _mrNotification = new();

        [GlobalSetup]
        public void Setup()
        {
            // --- FastMediator: nessun behavior (validazione disattivata -> fast-path) ---
            var fmServices = new ServiceCollection();
            fmServices.AddFastMediator(scan => scan.FromAssemblyOf<MediatRComparisonBenchmarks>(), o =>
            {
                o.RegistrationMode = HandlerRegistrationMode.Startup;
                o.EnableValidation = false;
            });
            // Lo scan dell'assembly registra anche i behavior open-generic usati dagli altri
            // benchmark: li rimuoviamo per isolare lo scenario "nessun behavior".
            RemoveFastMediatorBehaviors(fmServices);
            _fast = fmServices.BuildServiceProvider().GetRequiredService<Dispatcher>();

            // --- FastMediator: con un solo behavior no-op ---
            var fmServicesB = new ServiceCollection();
            fmServicesB.AddFastMediator(scan => scan.FromAssemblyOf<MediatRComparisonBenchmarks>(), o =>
            {
                o.RegistrationMode = HandlerRegistrationMode.Startup;
                o.EnableValidation = false;
            });
            RemoveFastMediatorBehaviors(fmServicesB);
            fmServicesB.AddTransient(typeof(FM.IPipelineBehaviorAsync<,>), typeof(FmNoOpBehavior<,>));
            _fastWithBehavior = fmServicesB.BuildServiceProvider().GetRequiredService<Dispatcher>();

            // --- MediatR: nessun behavior ---
            var mrServices = new ServiceCollection();
            mrServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRComparisonBenchmarks).Assembly));
            _mediatr = mrServices.BuildServiceProvider().GetRequiredService<MediatR.IMediator>();

            // --- MediatR: con un behavior no-op ---
            var mrServicesB = new ServiceCollection();
            mrServicesB.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(MediatRComparisonBenchmarks).Assembly);
                cfg.AddOpenBehavior(typeof(MrNoOpBehavior<,>));
            });
            _mediatrWithBehavior = mrServicesB.BuildServiceProvider().GetRequiredService<MediatR.IMediator>();
        }

        // Rimuove tutti i behavior della pipeline FastMediator (sync e async) registrati dallo scan.
        private static void RemoveFastMediatorBehaviors(IServiceCollection services)
        {
            var toRemove = services.Where(d =>
                d.ServiceType.IsGenericType &&
                (d.ServiceType.GetGenericTypeDefinition() == typeof(FM.IPipelineBehavior<,>) ||
                 d.ServiceType.GetGenericTypeDefinition() == typeof(FM.IPipelineBehaviorAsync<,>)))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }
        }

        // ----------------- Send: nessun behavior -----------------

        [Benchmark(Baseline = true), BenchmarkCategory("Send")]
        public int FastMediator_Send_Sync() => _fast.Send(_fmSync);

        [Benchmark, BenchmarkCategory("Send")]
        public Task<int> FastMediator_Send_Async() => _fast.SendAsync(_fmAsync);

        [Benchmark, BenchmarkCategory("Send")]
        public Task<int> MediatR_Send() => _mediatr.Send(_mrRequest);

        // ----------------- Send: con un behavior -----------------

        [Benchmark(Baseline = true), BenchmarkCategory("Send+Behavior")]
        public Task<int> FastMediator_Send_Async_1Behavior() => _fastWithBehavior.SendAsync(_fmAsync);

        [Benchmark, BenchmarkCategory("Send+Behavior")]
        public Task<int> MediatR_Send_1Behavior() => _mediatrWithBehavior.Send(_mrRequest);

        // ----------------- Publish -----------------

        [Benchmark(Baseline = true), BenchmarkCategory("Publish")]
        public void FastMediator_Publish() => _fast.Publish(_fmNotification);

        [Benchmark, BenchmarkCategory("Publish")]
        public Task MediatR_Publish() => _mediatr.Publish(_mrNotification);
    }

    // ====================== Modelli FastMediator ======================

    public sealed class FmRequest : FM.IRequest<int>
    {
        public int Value { get; set; }
    }

    public sealed class FmRequestHandler : FM.IRequestHandler<FmRequest, int>
    {
        public int Handle(FmRequest request) => request.Value + 1;
    }

    public sealed class FmAsyncRequest : FM.IAsyncRequest<int>
    {
        public int Value { get; set; }
    }

    public sealed class FmAsyncRequestHandler : FM.IAsyncRequestHandler<FmAsyncRequest, int>
    {
        public Task<int> HandleAsync(FmAsyncRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(request.Value + 1);
    }

    public sealed class FmNotification : FM.INotification { }

    public sealed class FmNotificationHandler : FM.INotificationHandler<FmNotification>
    {
        public void Handle(FmNotification notification) { }
    }

    // Behavior no-op: registrato manualmente (Scrutor non auto-registra gli open generic).
    public sealed class FmNoOpBehavior<TRequest, TResponse> : FM.IPipelineBehaviorAsync<TRequest, TResponse>
        where TRequest : FM.IAsyncRequest<TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<TRequest, CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
            => next(request, cancellationToken);
    }

    // ====================== Modelli MediatR ======================

    public sealed class MrRequest : MediatR.IRequest<int>
    {
        public int Value { get; set; }
    }

    public sealed class MrRequestHandler : MediatR.IRequestHandler<MrRequest, int>
    {
        public Task<int> Handle(MrRequest request, CancellationToken cancellationToken)
            => Task.FromResult(request.Value + 1);
    }

    public sealed class MrNotification : MediatR.INotification { }

    public sealed class MrNotificationHandler : MediatR.INotificationHandler<MrNotification>
    {
        public Task Handle(MrNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class MrNoOpBehavior<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
        where TRequest : MediatR.IRequest<TResponse>
    {
        public Task<TResponse> Handle(TRequest request, MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            => next();
    }
}
