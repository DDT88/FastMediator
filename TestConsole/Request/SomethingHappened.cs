using FastMediator.Interfaces;

public class SomethingHappened : INotification
{
    public string Message { get; set; } = "";
}