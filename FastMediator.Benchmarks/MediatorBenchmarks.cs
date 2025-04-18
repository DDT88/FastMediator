using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMediator.Benchmarks.Models;
using FastMediator.Configuration;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FastMediator.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class MediatorBenchmarks
    {
        private Dispatcher _startupMediator;
        private Dispatcher _lazyMediator;
        private Dispatcher _hybridMediator;

        [GlobalSetup]
        public void Setup()
        {
            // Configura i tre mediator con le diverse modalità di registrazione
            _startupMediator = InitializeMediator(HandlerRegistrationMode.Startup);
            _lazyMediator = InitializeMediator(HandlerRegistrationMode.LazyLoading);
            _hybridMediator = InitializeMediator(HandlerRegistrationMode.Hybrid, typeof(PingRequest));

            // Warmup
            _startupMediator.Send(new PingRequest("Warmup"));
            _lazyMediator.Send(new PingRequest("Warmup"));
            _hybridMediator.Send(new PingRequest("Warmup"));

            // Warmup per gli handler asincroni
            _startupMediator.SendAsync(new AsyncPingRequest("Warmup")).GetAwaiter().GetResult();
            _lazyMediator.SendAsync(new AsyncPingRequest("Warmup")).GetAwaiter().GetResult();
            _hybridMediator.SendAsync(new AsyncPingRequest("Warmup")).GetAwaiter().GetResult();
        }

        private Dispatcher InitializeMediator(HandlerRegistrationMode mode, params Type[] warmupTypes)
        {
            var services = new ServiceCollection();

            services.AddCustomMediator(scan => scan.FromAssemblyOf<MediatorBenchmarks>(), options =>
            {
                options.RegistrationMode = mode;

                foreach (var type in warmupTypes)
                    options.WarmupTypes.Add(type);

                // Disabilita i behaviors che non ci interessano per i benchmark
                options.EnableDiagnostics = false;
                options.EnableTiming = false;
                options.EnableDetailedLogging = false;
            });

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<Dispatcher>();
        }

        #region Benchmark Richieste Sincrone

        [Benchmark]
        [BenchmarkCategory("Sync", "Startup")]
        public string Startup_SingleRequest()
        {
            return _startupMediator.Send(new PingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Lazy")]
        public string Lazy_SingleRequest()
        {
            return _lazyMediator.Send(new PingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Hybrid")]
        public string Hybrid_SingleRequest()
        {
            return _hybridMediator.Send(new PingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Startup")]
        [Arguments(10)]
        [Arguments(100)]
        public void Startup_MultipleRequests(int count)
        {
            for (int i = 0; i < count; i++)
                _startupMediator.Send(new PingRequest($"Test {i}"));
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Lazy")]
        [Arguments(10)]
        [Arguments(100)]
        public void Lazy_MultipleRequests(int count)
        {
            for (int i = 0; i < count; i++)
                _lazyMediator.Send(new PingRequest($"Test {i}"));
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Hybrid")]
        [Arguments(10)]
        [Arguments(100)]
        public void Hybrid_MultipleRequests(int count)
        {
            for (int i = 0; i < count; i++)
                _hybridMediator.Send(new PingRequest($"Test {i}"));
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Startup")]
        public void Startup_MixedRequests()
        {
            _startupMediator.Send(new PingRequest("Test"));
            _startupMediator.Send(new ComplexRequest { Value = 10 });
            _startupMediator.Publish(new SomethingHappened { Message = "Event" });
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Lazy")]
        public void Lazy_MixedRequests()
        {
            _lazyMediator.Send(new PingRequest("Test"));
            _lazyMediator.Send(new ComplexRequest { Value = 10 });
            _lazyMediator.Publish(new SomethingHappened { Message = "Event" });
        }

        [Benchmark]
        [BenchmarkCategory("Sync", "Hybrid")]
        public void Hybrid_MixedRequests()
        {
            _hybridMediator.Send(new PingRequest("Test"));
            _hybridMediator.Send(new ComplexRequest { Value = 10 });
            _hybridMediator.Publish(new SomethingHappened { Message = "Event" });
        }

        #endregion

        #region Benchmark Richieste Asincrone

        [Benchmark]
        [BenchmarkCategory("Async", "Startup")]
        public async Task<string> Startup_SingleRequestAsync()
        {
            return await _startupMediator.SendAsync(new AsyncPingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Lazy")]
        public async Task<string> Lazy_SingleRequestAsync()
        {
            return await _lazyMediator.SendAsync(new AsyncPingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Hybrid")]
        public async Task<string> Hybrid_SingleRequestAsync()
        {
            return await _hybridMediator.SendAsync(new AsyncPingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Startup")]
        [Arguments(10)]
        [Arguments(100)]
        public async Task Startup_MultipleRequestsAsync(int count)
        {
            for (int i = 0; i < count; i++)
                await _startupMediator.SendAsync(new AsyncPingRequest($"Test {i}"));
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Lazy")]
        [Arguments(10)]
        [Arguments(100)]
        public async Task Lazy_MultipleRequestsAsync(int count)
        {
            for (int i = 0; i < count; i++)
                await _lazyMediator.SendAsync(new AsyncPingRequest($"Test {i}"));
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Hybrid")]
        [Arguments(10)]
        [Arguments(100)]
        public async Task Hybrid_MultipleRequestsAsync(int count)
        {
            for (int i = 0; i < count; i++)
                await _hybridMediator.SendAsync(new AsyncPingRequest($"Test {i}"));
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Startup")]
        public async Task Startup_MixedRequestsAsync()
        {
            await _startupMediator.SendAsync(new AsyncPingRequest("Test"));
            await _startupMediator.PublishAsync(new AsyncSomethingHappened { Message = "Event" });
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Lazy")]
        public async Task Lazy_MixedRequestsAsync()
        {
            await _lazyMediator.SendAsync(new AsyncPingRequest("Test"));
            await _lazyMediator.PublishAsync(new AsyncSomethingHappened { Message = "Event" });
        }

        [Benchmark]
        [BenchmarkCategory("Async", "Hybrid")]
        public async Task Hybrid_MixedRequestsAsync()
        {
            await _hybridMediator.SendAsync(new AsyncPingRequest("Test"));
            await _hybridMediator.PublishAsync(new AsyncSomethingHappened { Message = "Event" });
        }

        #endregion

        #region Confronto Sync vs Async

        [Benchmark]
        [BenchmarkCategory("Comparison")]
        public string Sync_Request()
        {
            return _startupMediator.Send(new PingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Comparison")]
        public async Task<string> Async_Request()
        {
            return await _startupMediator.SendAsync(new AsyncPingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Comparison")]
        public void Sync_Notification()
        {
            _startupMediator.Publish(new SomethingHappened { Message = "Test" });
        }

        [Benchmark]
        [BenchmarkCategory("Comparison")]
        public async Task Async_Notification()
        {
            await _startupMediator.PublishAsync(new AsyncSomethingHappened { Message = "Test" });
        }

        #endregion

        #region Interoperabilità

        [Benchmark]
        [BenchmarkCategory("Interop")]
        public async Task SyncToAsync_Request()
        {
            await _startupMediator.SendAsAsync<PingRequest, string>(new PingRequest("Test"));
        }

        [Benchmark]
        [BenchmarkCategory("Interop")]
        public string AsyncToSync_Request()
        {
            return _startupMediator.SendSync<AsyncPingRequest, string>(new AsyncPingRequest("Test"));
        }

        #endregion
    }
}
