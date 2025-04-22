using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using FastMediator.Configuration;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using TestConsole;

public class Program
{
    public static void Main(string[] args)
    {
        // Se vuoi solo eseguire il test base, decommenta questo
       RunBasicTest();


    }

    private static void RunBasicTest()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine($"Avvio elaborazione");

        var services = new ServiceCollection();


        // Aggiungi il logging di base
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Ottieni il LoggerFactory
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        services.AddCustomMediator(scan => scan.FromAssemblyOf<Program>(),options =>
        {
            options.EnableDiagnostics = false;
            options.EnableTiming = true;
            options.EnableDetailedLogging = false;
            options.LoggerFactory = loggerFactory; // Passa il LoggerFactory alla configurazione
            options.UseHybridMode().WithWarmup<PingRequest>();
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Dispatcher>();
       
      
        try
        {

            for (int i = 0; i < 10; i++)
            {
                mediator.Send(new PingRequest($"Test {i}"));
            }

            var stats = mediator.GetRequestHandlerCacheStats();
          //  Console.WriteLine($"Dopo 10 richieste - Cache hits: {stats.Hits}, misses: {stats.Misses}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
        stopwatch.Stop();
        Console.WriteLine($"[Timing] Completata elaborazione in {stopwatch.ElapsedMilliseconds}ms");
        //  mediator.Publish(new SomethingHappened { Message = "Boom" });




    }
}
