using FastMediator.Core;
using FastMediator.Configuration;
using FastMediator.DependencyInjection;
using FastMediator.Interfaces;
using FastMediator.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using System;

namespace FastMediator.UnitTests
{
    public class ValidationTests
    {
        [Fact]
        public void Send_WithInvalidRequest_ThrowsValidationException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddFastMediator(scan => scan.FromAssemblyOf<ValidationTests>());
            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<Dispatcher>();
            var request = new ValidatedRequest { Name = "" }; // Invalid: Name is required

            // Act & Assert
            Assert.Throws<ValidationException>(() => dispatcher.Send(request));
        }

        [Fact]
        public void Send_WithValidRequest_ReturnsCorrectResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddFastMediator(scan => scan.FromAssemblyOf<ValidationTests>());
            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<Dispatcher>();
            var request = new ValidatedRequest { Name = "John", Age = 25 };

            // Act
            var response = dispatcher.Send(request);

            // Assert
            response.Should().Be("Name: John, Age: 25");
        }

        [Fact]
        public void ValidationResult_WithMultipleErrors_ContainsAllErrors()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddFastMediator(scan => scan.FromAssemblyOf<ValidationTests>());
            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<Dispatcher>();
            var request = new ValidatedRequest { Name = "", Age = -5 }; // Multiple validation errors

            // Act
            var exception = Assert.Throws<ValidationException>(() => dispatcher.Send(request));

            // Assert
            exception.Errors.Should().HaveCount(2);
            exception.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required");
            exception.Errors.Should().Contain(e => e.PropertyName == "Age" && e.ErrorMessage == "Age must be positive");
        }

        // Helper classes for tests
        public class ValidatedRequest : IRequest<string>
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class ValidatedRequestValidator : AbstractValidator<ValidatedRequest>
        {
            protected override void ValidateInternal(ValidatedRequest instance, ValidationResult result)
            {
                if (string.IsNullOrEmpty(instance.Name))
                {
                    result.AddError("Name", "Name is required");
                }

                if (instance.Age < 0)
                {
                    result.AddError("Age", "Age must be positive");
                }
            }
        }

        public class ValidatedRequestHandler : IRequestHandler<ValidatedRequest, string>
        {
            public string Handle(ValidatedRequest request)
            {
                return $"Name: {request.Name}, Age: {request.Age}";
            }
        }
    }
}
