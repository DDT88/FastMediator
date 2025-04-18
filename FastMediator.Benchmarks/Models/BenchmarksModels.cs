using FastMediator.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace FastMediator.Benchmarks.Models
{
    // Request/Response models for benchmarking
    public class PingRequest : IRequest<string>
    {
        public string Message { get; }

        public PingRequest(string message)
        {
            Message = message;
        }
    }

    public class PingHandler : IRequestHandler<PingRequest, string>
    {
        public string Handle(PingRequest request)
        {
            return $"Risposta a: {request.Message}";
        }
    }

    public class ComplexRequest : IRequest<int>
    {
        public int Value { get; set; }
    }

    public class ComplexRequestHandler : IRequestHandler<ComplexRequest, int>
    {
        public int Handle(ComplexRequest request)
        {
            return request.Value * 2;
        }
    }

    public class SomethingHappened : INotification
    {
        public string Message { get; set; } = "";
    }

    public class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
    {
        public void Handle(SomethingHappened notification)
        {
            // Intenzionalmente vuoto per il benchmark
        }
    }

    // Async models
    public class AsyncPingRequest : IAsyncRequest<string>
    {
        public string Message { get; }

        public AsyncPingRequest(string message)
        {
            Message = message;
        }
    }

    public class AsyncPingHandler : IAsyncRequestHandler<AsyncPingRequest, string>
    {
        public Task<string> HandleAsync(AsyncPingRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"Risposta asincrona a: {request.Message}");
        }
    }

    public class AsyncSomethingHappened : IAsyncNotification
    {
        public string Message { get; set; } = "";
    }

    public class AsyncSomethingHappenedHandler : IAsyncNotificationHandler<AsyncSomethingHappened>
    {
        public Task HandleAsync(AsyncSomethingHappened notification, CancellationToken cancellationToken = default)
        {
            // Intenzionalmente vuoto per il benchmark
            return Task.CompletedTask;
        }
    }

    // Modelli per benchmark con pipeline complessa
    public class ComplexProcessRequest : IRequest<ProcessResult>
    {
        public string Data { get; set; }
        public int Priority { get; set; }
    }

    public class ProcessResult
    {
        public string ProcessedData { get; set; }
        public bool Success { get; set; }
        public int ProcessingTime { get; set; }
    }

    public class ComplexProcessRequestHandler : IRequestHandler<ComplexProcessRequest, ProcessResult>
    {
        public ProcessResult Handle(ComplexProcessRequest request)
        {
            // Simulazione di elaborazione
            var processingTime = request.Priority switch
            {
                0 => 10,
                1 => 5,
                _ => 1
            };

            return new ProcessResult
            {
                ProcessedData = $"Processed: {request.Data}",
                Success = true,
                ProcessingTime = processingTime
            };
        }
    }
}