using FastMediator;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCustomMediator();



var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<Dispatcher>();

try
{
    var result = mediator.Send(new Ping ("Hello Mediator!" ));
    Console.WriteLine($"Risultato: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] {ex.Message}");
}

mediator.Publish(new SomethingHappened { Message = "Boom" });


// ----- Classes di esempio -----
public class Ping(string message) : IRequest<string>
{
    public string Message { get;  } = message;
}

public class PingHandler : IRequestHandler<Ping, string>
{
    public string Handle(Ping request) => $"Risposta a: {request.Message}";
}

public class SomethingHappened : INotification
{
    public string Message { get; set; } = "";
}

public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
{
    public void Handle(SomethingHappened notification)
    {
        Console.WriteLine($"Evento gestito: {notification.Message}");
    }
}