using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static void Main(string[] args)
    {
        // Se vuoi solo eseguire il test base, decommenta questo
       RunBasicTest();

        // Esegui il benchmark completo
       // var summary = BenchmarkRunner.Run<MediatorBenchmarks>();
        Console.WriteLine("Benchmark completato!");
    }

    private static void RunBasicTest()
    {
        var services = new ServiceCollection();
        services.AddCustomMediator(scan => scan.FromAssemblyOf<Program>(),options =>
        {
            options.EnableDiagnostics = true;
            options.EnableTiming = true;
            options.EnableDetailedLogging = true;
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Dispatcher>();

        try
        {
            var result = mediator.Send(new Ping("Hello Mediator!"));
            Console.WriteLine($"Risultato: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }

        mediator.Publish(new SomethingHappened { Message = "Boom" });
    }
}

[MemoryDiagnoser]  // Questo misura anche l'allocazione di memoria
public class MediatorBenchmarks
{
    private IServiceProvider _serviceProvider;
    private Dispatcher _mediator;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddCustomMediator(scan => scan.FromAssemblyOf<MediatorBenchmarks>());
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<Dispatcher>();
    }

    [Benchmark(Baseline = true)]
    public string SendSingleRequest()
    {
        return _mediator.Send(new Ping("Benchmark test"));
    }

    [Benchmark]
    public void PublishSingleNotification()
    {
        _mediator.Publish(new SomethingHappened { Message = "Benchmark notification" });
    }

    [Benchmark]
    public string[] SendMultipleRequests()
    {
        var results = new string[100];
        for (int i = 0; i < 100; i++)
        {
            results[i] = _mediator.Send(new Ping($"Request {i}"));
        }
        return results;
    }

    [Benchmark]
    public void SendRequestsWithDifferentTypes()
    {
        _mediator.Send(new Ping("First request"));
        _mediator.Send(new ComplexRequest { Value = 42 });
        _mediator.Send(new AnotherRequest { Name = "Test" });
    }

    [Benchmark]
    public void MixedOperations()
    {
        _mediator.Send(new Ping("Mixed operation"));
        _mediator.Publish(new SomethingHappened { Message = "Event 1" });
        _mediator.Send(new ComplexRequest { Value = 100 });
        _mediator.Publish(new AnotherEvent { Id = 1 });
    }
}