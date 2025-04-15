using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

public class Program
{
    public static void Main(string[] args)
    {
        // Se vuoi solo eseguire il test base, decommenta questo
       RunBasicTest();

        // Esegui il benchmark completo
        //var summary = BenchmarkRunner.Run<MediatorBenchmarks>();
        Console.WriteLine("Benchmark completato!");
    }

    private static void RunBasicTest()
    {
        var services = new ServiceCollection();


        // Aggiungi il logging di base
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Ottieni il LoggerFactory
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        services.AddCustomMediator(scan => scan.FromAssemblyOf<Program>(),options =>
        {
            options.EnableDiagnostics = false;
            options.EnableTiming = false;
            options.EnableDetailedLogging = true;
            options.LoggerFactory = loggerFactory; // Passa il LoggerFactory alla configurazione
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Dispatcher>();

        try
        {
            var result = mediator.Send(new Ping("ciao"));
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
        services.AddCustomMediator(scan => scan.FromAssemblyOf<MediatorBenchmarks>(), options =>
        {
            options.EnableDiagnostics = true;
            options.EnableTiming = true;
            options.EnableDetailedLogging = true;
        });

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