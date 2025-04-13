using FastMediator.Interfaces;

public class AnotherRequest : IRequest<bool>
{
    public string Name { get; set; }
}

public class AnotherRequestHandler : IRequestHandler<AnotherRequest, bool>
{
    public bool Handle(AnotherRequest request) => !string.IsNullOrEmpty(request.Name);
}
