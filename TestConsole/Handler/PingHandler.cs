using FastMediator.Interfaces;

public class PingHandler : IRequestHandler<PingRequest, string>
{
    public string Handle(PingRequest request) => $"Risposta a: {request.Message}";
}