using BenchmarkDotNet.Loggers;
using FastMediator.Interfaces;
using Microsoft.Extensions.Logging;

public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
{
    private ILogger<SomethingHappenedHandler> _logger;

    public void Handle(SomethingHappened notification)
    {
        _logger.LogInformation($"Evento gestito: {notification.Message}");
    }
}