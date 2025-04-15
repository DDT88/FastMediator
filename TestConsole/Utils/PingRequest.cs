using FastMediator.Interfaces;

public class PingRequest(string message) : IRequest<string>
{
    public string Message { get; } = message;
}