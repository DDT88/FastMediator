using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMediator.Benchmarks.Behaviors;
using FastMediator.Benchmarks.Models;
using FastMediator.Configuration;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FastMediator.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class AdvancedMediatorBenchmarks
    {
        private Dispatcher _noBehaviorsMediator;
        private Dispatcher _lightweightBehaviorsMediator;
        private Dispatcher _standardBehaviorsMediator;
        private Dispatcher _heavyBehaviorsMediator;
        private Dispatcher _fullPipelineMediator;

        [GlobalSetup]
        public void Setup()
        {
            // Diversi tipi di mediator con complessità crescente
            _noBehaviorsMediator = InitializeMediator(registerBehaviors: false);
            _lightweightBehaviorsMediator = InitializeMediator(registerBehaviors: true, behaviorType: BehaviorType.Lightweight);
            _standardBehaviorsMediator = InitializeMediator(registerBehaviors: true, behaviorType: BehaviorType.Standard);
            _heavyBehaviorsMediator = InitializeMediator(registerBehaviors: true, behaviorType: BehaviorType.Heavy);
            _fullPipelineMediator = InitializeMediator(registerBehaviors: true, behaviorType: BehaviorType.All);

            // Warmup
            _noBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Warmup", Priority = 1 });
            _lightweightBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Warmup", Priority = 1 });
            _standardBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Warmup", Priority = 1 });
            _heavyBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Warmup", Priority = 1 });
            _fullPipelineMediator.Send(new ComplexProcessRequest { Data = "Warmup", Priority = 1 });
        }

        private enum BehaviorType
        {
            None,
            Lightweight,
            Standard,
            Heavy,
            All
        }

        private Dispatcher InitializeMediator(bool registerBehaviors = false, BehaviorType behaviorType = BehaviorType.None)
        {
            var services = new ServiceCollection();

            // Registra i servizi base
            services.AddFastMediator(scan => scan.FromAssemblyOf<AdvancedMediatorBenchmarks>(), options =>
            {
                options.RegistrationMode = HandlerRegistrationMode.Startup;
                options.EnableDiagnostics = false;
                options.EnableTiming = false;
                options.EnableDetailedLogging = false;
            });

            // Aggiunge i behavior specifici in base alla configurazione
            if (registerBehaviors)
            {
                switch (behaviorType)
                {
                    case BehaviorType.Lightweight:
                        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LightweightBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(AsyncLightweightBehavior<,>));
                        break;

                    case BehaviorType.Standard:
                        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(StandardBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(AsyncStandardBehavior<,>));
                        break;

                    case BehaviorType.Heavy:
                        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HeavyBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(AsyncHeavyBehavior<,>));
                        break;

                    case BehaviorType.All:
                        // Registra tutti i behavior insieme
                        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LightweightBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(StandardBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HeavyBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(BenchmarkMonitoringBehavior<,>));

                        services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(AsyncLightweightBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(AsyncStandardBehavior<,>));
                        services.AddTransient(typeof(IPipelineBehaviorAsync<,>), typeof(AsyncHeavyBehavior<,>));
                        break;
                }
            }

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<Dispatcher>();
        }

        #region Pipeline Complexity Benchmarks

        [Benchmark]
        [BenchmarkCategory("Pipeline", "NoBehaviors")]
        public ProcessResult NoBehaviors_ProcessRequest()
        {
            return _noBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Test", Priority = 1 });
        }

        [Benchmark]
        [BenchmarkCategory("Pipeline", "Lightweight")]
        public ProcessResult LightweightBehaviors_ProcessRequest()
        {
            return _lightweightBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Test", Priority = 1 });
        }

        [Benchmark]
        [BenchmarkCategory("Pipeline", "Standard")]
        public ProcessResult StandardBehaviors_ProcessRequest()
        {
            return _standardBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Test", Priority = 1 });
        }

        [Benchmark]
        [BenchmarkCategory("Pipeline", "Heavy")]
        public ProcessResult HeavyBehaviors_ProcessRequest()
        {
            return _heavyBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Test", Priority = 1 });
        }

        [Benchmark]
        [BenchmarkCategory("Pipeline", "FullPipeline")]
        public ProcessResult FullPipeline_ProcessRequest()
        {
            return _fullPipelineMediator.Send(new ComplexProcessRequest { Data = "Test", Priority = 1 });
        }

        #endregion

        #region Priority Benchmarks

        [Benchmark]
        [BenchmarkCategory("Priority")]
        [Arguments(0)] // Alta priorità (più lento da processare)
        [Arguments(1)] // Media priorità
        [Arguments(2)] // Bassa priorità (più veloce da processare)
        public ProcessResult ProcessWithPriority(int priority)
        {
            return _fullPipelineMediator.Send(new ComplexProcessRequest { Data = "Test", Priority = priority });
        }

        #endregion

        #region Throughput Benchmarks

        [Benchmark]
        [BenchmarkCategory("Throughput")]
        [Arguments(10)]
        [Arguments(50)]
        [Arguments(100)]
        public void HighThroughput_NoTracking(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _noBehaviorsMediator.Send(new PingRequest($"Test {i}"));
            }
        }

        [Benchmark]
        [BenchmarkCategory("Throughput")]
        [Arguments(10)]
        [Arguments(50)]
        [Arguments(100)]
        public void HighThroughput_WithTracking(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _fullPipelineMediator.Send(new PingRequest($"Test {i}"));
            }
        }

        #endregion

        #region Parallel Processing

        [Benchmark]
        [BenchmarkCategory("Parallel")]
        [Arguments(10)]
        [Arguments(50)]
        [Arguments(100)]
        public void ParallelProcessing(int count)
        {
            var requests = Enumerable.Range(0, count)
                .Select(i => new PingRequest($"Parallel {i}"))
                .ToArray();

            Parallel.ForEach(requests, request =>
            {
                _noBehaviorsMediator.Send(request);
            });
        }

        [Benchmark]
        [BenchmarkCategory("Parallel")]
        [Arguments(10)]
        [Arguments(50)]
        [Arguments(100)]
        public async Task ParallelProcessingAsync(int count)
        {
            var requests = Enumerable.Range(0, count)
                .Select(i => new AsyncPingRequest($"Parallel {i}"))
                .ToArray();

            var tasks = requests.Select(request => _noBehaviorsMediator.SendAsync(request));

            await Task.WhenAll(tasks);
        }

        #endregion

        #region Cold Start vs Warm Benchmarks

        [Benchmark]
        [BenchmarkCategory("ColdWarm")]
        public Dispatcher ColdStart_Initialization()
        {
            // Misura il tempo per inizializzare un nuovo mediator
            return InitializeMediator(registerBehaviors: true, behaviorType: BehaviorType.Standard);
        }

        [Benchmark]
        [BenchmarkCategory("ColdWarm")]
        public ProcessResult ColdStart_FirstRequest()
        {
            // Crea un nuovo mediator e invia immediatamente una richiesta
            var mediator = InitializeMediator(registerBehaviors: true, behaviorType: BehaviorType.Standard);
            return mediator.Send(new ComplexProcessRequest { Data = "Cold", Priority = 1 });
        }

        [Benchmark]
        [BenchmarkCategory("ColdWarm")]
        public ProcessResult WarmStart_Request()
        {
            // Usa un mediator già inizializzato
            return _standardBehaviorsMediator.Send(new ComplexProcessRequest { Data = "Warm", Priority = 1 });
        }

        #endregion
    }
}
