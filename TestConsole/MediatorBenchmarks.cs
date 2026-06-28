using BenchmarkDotNet.Attributes;
using FastMediator.Configuration;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TestConsole.Request;


namespace TestConsole
{
    public class MediatorBenchmarks
    {
        private Dispatcher _startupMediator;
        private Dispatcher _lazyMediator;
        private Dispatcher _hybridMediator;

        [GlobalSetup]
        public void Setup()
        {
            // Configura i tre mediator con le diverse modalità di registrazione
            _startupMediator = InitializeStartupMediator();
            _lazyMediator = InitializeLazyMediator();
            _hybridMediator = InitializeHybridMediator();
            // Warmup per gli handler asincroni
            _ = _startupMediator.SendAsync(new AsyncPingRequest("Warmup")).GetAwaiter().GetResult();
            _ = _lazyMediator.SendAsync(new AsyncPingRequest("Warmup")).GetAwaiter().GetResult();
            _ = _hybridMediator.SendAsync(new AsyncPingRequest("Warmup")).GetAwaiter().GetResult();

        }

        [Benchmark]
        public Dispatcher InitializeStartupMediator()
        {
            return ConfigureMediator(HandlerRegistrationMode.Startup);
        }

        [Benchmark]
        public Dispatcher InitializeLazyMediator()
        {
            return ConfigureMediator(HandlerRegistrationMode.LazyLoading);
        }

        [Benchmark]
        public Dispatcher InitializeHybridMediator()
        {
            return ConfigureMediator(HandlerRegistrationMode.Hybrid, typeof(PingRequest));
        }

        private Dispatcher ConfigureMediator(HandlerRegistrationMode mode, params Type[] warmupTypes)
        {
            var services = new ServiceCollection();

            services.AddFastMediator(scan => scan.FromAssemblyOf<MediatorBenchmarks>(), options =>
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

        [Benchmark]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(100)]
        public void Startup_SendPing(int iterations)
        {
            for (int i = 0; i < iterations; i++)
                _startupMediator.Send(new PingRequest($"Test {i}"));
        }

        [Benchmark]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(100)]
        public void Lazy_SendPing(int iterations)
        {
            for (int i = 0; i < iterations; i++)
                _lazyMediator.Send(new PingRequest($"Test {i}"));
        }

        [Benchmark]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(100)]
        public void Hybrid_SendPing(int iterations)
        {
            for (int i = 0; i < iterations; i++)
                _hybridMediator.Send(new PingRequest($"Test {i}"));
        }

        // BENCHMARK ASINCRONI


        [Benchmark]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(100)]
        public async Task Startup_SendPingAsync(int iterations)
        {
            for (int i = 0; i < iterations; i++)
                await _startupMediator.SendAsync(new AsyncPingRequest($"Test {i}"));
        }

        [Benchmark]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(100)]
        public async Task Lazy_SendPingAsync(int iterations)
        {
            for (int i = 0; i < iterations; i++)
                await _lazyMediator.SendAsync(new AsyncPingRequest($"Test {i}"));
        }

        [Benchmark]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(100)]
        public async Task Hybrid_SendPingAsync(int iterations)
        {
            for (int i = 0; i < iterations; i++)
                await _hybridMediator.SendAsync(new AsyncPingRequest($"Test {i}"));
        }



        // Test con richieste miste
        [Benchmark]
        public void Startup_MixedRequests()
        {
            _startupMediator.Send(new PingRequest("Test"));
            _startupMediator.Send(new ComplexRequest { Value = 10 });
            _startupMediator.Send(new AnotherRequest { Name = "Test" });
        }

        [Benchmark]
        public void Lazy_MixedRequests()
        {
            _lazyMediator.Send(new PingRequest("Test"));
            _lazyMediator.Send(new ComplexRequest { Value = 10 });
            _lazyMediator.Send(new AnotherRequest { Name = "Test" });
        }

        [Benchmark]
        public void Hybrid_MixedRequests()
        {
            _hybridMediator.Send(new PingRequest("Test"));
            _hybridMediator.Send(new ComplexRequest { Value = 10 });
            _hybridMediator.Send(new AnotherRequest { Name = "Test" });
        }

        // TEST CON RICHIESTE MISTE ASINCRONE

        [Benchmark]
        public async Task Startup_MixedRequestsAsync()
        {
            await _startupMediator.SendAsync(new AsyncPingRequest("Test"));
            await _startupMediator.SendAsync(new AsyncPingRequest("Test2"));
            await _startupMediator.PublishAsync(new AsyncSomethingHappened { Message = "Test" });
        }

        [Benchmark]
        public async Task Lazy_MixedRequestsAsync()
        {
            await _lazyMediator.SendAsync(new AsyncPingRequest("Test"));
            await _lazyMediator.SendAsync(new AsyncPingRequest("Test2"));
            await _lazyMediator.PublishAsync(new AsyncSomethingHappened { Message = "Test" });
        }

        [Benchmark]
        public async Task Hybrid_MixedRequestsAsync()
        {
            await _hybridMediator.SendAsync(new AsyncPingRequest("Test"));
            await _hybridMediator.SendAsync(new AsyncPingRequest("Test2"));
            await _hybridMediator.PublishAsync(new AsyncSomethingHappened { Message = "Test" });
        }

        // CONFRONTO SYNC VS ASYNC

        [Benchmark]
        public void Sync_SingleRequest()
        {
            _startupMediator.Send(new PingRequest("Test"));
        }

        [Benchmark]
        public async Task Async_SingleRequest()
        {
            await _startupMediator.SendAsync(new AsyncPingRequest("Test"));
        }

        [Benchmark]
        public void Sync_Notification()
        {
            _startupMediator.Publish(new SomethingHappened { Message = "Test" });
        }

        [Benchmark]
        public async Task Async_Notification()
        {
            await _startupMediator.PublishAsync(new AsyncSomethingHappened { Message = "Test" });
        }

        // TEST DI INTEROPERABILITÀ

        [Benchmark]
        public async Task SyncToAsync_Request()
        {
            // Usa l'estensione SendAsAsync per inviare una richiesta sincrona in modo asincrono
            await _startupMediator.SendAsAsync<PingRequest, string>(new PingRequest("Test"));
        }

        [Benchmark]
        public void AsyncToSync_Request()
        {
            // Usa l'estensione SendSync per inviare una richiesta asincrona in modo sincrono
            _startupMediator.SendSync<AsyncPingRequest, string>(new AsyncPingRequest("Test"));
        }
    }
}

