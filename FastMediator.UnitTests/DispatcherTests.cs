using FastMediator.Core;
using FastMediator.Configuration;
using FastMediator.DependencyInjection;
using FastMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;

namespace FastMediator.UnitTests
{
    public class DispatcherTests
    {
        [Fact]
        public void Send_WithValidRequest_ReturnsCorrectResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddFastMediator(scan => scan.FromAssemblyOf<DispatcherTests>());
            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<Dispatcher>();
            var request = new TestRequest("Hello World");

            // Act
            var response = dispatcher.Send(request);

            // Assert
            response.Should().Be("Response: Hello World");
        }

        [Fact]
        public async Task SendAsync_WithValidRequest_ReturnsCorrectResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddFastMediator(scan => scan.FromAssemblyOf<DispatcherTests>());
            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<Dispatcher>();
            var request = new TestAsyncRequest("Hello Async World");

            // Act
            var response = await dispatcher.SendAsync(request);

            // Assert
            response.Should().Be("Async Response: Hello Async World");
        }

        [Fact]
        public void Publish_WithNotification_CallsAllHandlers()
        {
            // Arrange
            NotificationCounter.Reset();

            var services = new ServiceCollection();
            services.AddFastMediator(scan => scan.FromAssemblyOf<DispatcherTests>());
            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<Dispatcher>();
            var notification = new TestNotification("Test Event");

            // Act
            dispatcher.Publish(notification);

            // Assert
            NotificationCounter.HandlerOneCalled.Should().BeTrue();
            NotificationCounter.HandlerTwoCalled.Should().BeTrue();
            NotificationCounter.Message.Should().Be("Test Event");
        }

        // Helper classes for tests
        public class TestRequest : IRequest<string>
        {
            public string Message { get; }

            public TestRequest(string message)
            {
                Message = message;
            }
        }

        public class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            public string Handle(TestRequest request)
            {
                return $"Response: {request.Message}";
            }
        }

        public class TestAsyncRequest : IAsyncRequest<string>
        {
            public string Message { get; }

            public TestAsyncRequest(string message)
            {
                Message = message;
            }
        }

        public class TestAsyncRequestHandler : IAsyncRequestHandler<TestAsyncRequest, string>
        {
            public Task<string> HandleAsync(TestAsyncRequest request, System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.FromResult($"Async Response: {request.Message}");
            }
        }

        public class TestNotification : INotification
        {
            public string Message { get; }

            public TestNotification(string message)
            {
                Message = message;
            }
        }

        public static class NotificationCounter
        {
            public static bool HandlerOneCalled { get;  set; }
            public static bool HandlerTwoCalled { get;  set; }
            public static string Message { get;  set; }

            public static void Reset()
            {
                HandlerOneCalled = false;
                HandlerTwoCalled = false;
                Message = null;
            }
        }

        public class TestNotificationHandlerOne : INotificationHandler<TestNotification>
        {
            public void Handle(TestNotification notification)
            {
                NotificationCounter.HandlerOneCalled = true;
                NotificationCounter.Message = notification.Message;
            }
        }

        public class TestNotificationHandlerTwo : INotificationHandler<TestNotification>
        {
            public void Handle(TestNotification notification)
            {
                NotificationCounter.HandlerTwoCalled = true;
            }
        }
    }
}

