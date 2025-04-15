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

            services.AddCustomMediator(scan => scan.FromAssemblyOf<MediatorBenchmarks>(), options =>
            {
                options.RegistrationMode = mode;
                foreach (var type in warmupTypes)
                    options.WarmupTypes.Add(type);
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
    }
}
