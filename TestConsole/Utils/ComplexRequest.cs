using FastMediator.Interfaces;

public class ComplexRequest : IRequest<int>
{
    public int Value { get; set; }
}

public class ComplexRequestHandler : IRequestHandler<ComplexRequest, int>
{
    public int Handle(ComplexRequest request) => request.Value * 2;
}