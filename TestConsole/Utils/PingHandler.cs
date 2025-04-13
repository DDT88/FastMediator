using FastMediator.Interfaces;

public class PingHandler : IRequestHandler<Ping, string>
{
    public string Handle(Ping request) => $"Risposta a: {request.Message}";
}