using FastMediator.Interfaces;

public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
{
    public void Handle(SomethingHappened notification)
    {
        Console.WriteLine($"Evento gestito: {notification.Message}");
    }
}