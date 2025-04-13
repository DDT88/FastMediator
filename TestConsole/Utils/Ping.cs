using FastMediator.Interfaces;

public class Ping(string message) : IRequest<string>
{
    public string Message { get; } = message;
}