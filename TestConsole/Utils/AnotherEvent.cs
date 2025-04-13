using FastMediator.Interfaces;

public class AnotherEvent : INotification
{
    public int Id { get; set; }
}

public class AnotherEventHandler : INotificationHandler<AnotherEvent>
{
    public void Handle(AnotherEvent notification)
    {
        // Intenzionalmente vuoto per il benchmark
    }
}