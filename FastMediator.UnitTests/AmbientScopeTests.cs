using System;
using FastMediator.Core;
using FastMediator.DependencyInjection;
using FastMediator.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FastMediator.UnitTests
{
    public class AmbientScopeTests
    {
        public interface IScopedDependency
        {
            Guid Id { get; }
        }

        public sealed class ScopedDependency : IScopedDependency
        {
            public Guid Id { get; } = Guid.NewGuid();
        }

        public sealed class ScopeProbeRequest : IRequest<Guid> { }

        public sealed class ScopeProbeHandler : IRequestHandler<ScopeProbeRequest, Guid>
        {
            private readonly IScopedDependency _dependency;
            public ScopeProbeHandler(IScopedDependency dependency) => _dependency = dependency;
            public Guid Handle(ScopeProbeRequest request) => _dependency.Id;
        }

        [Fact]
        public void Send_ResolvesHandlerDependenciesFromCurrentScope()
        {
            var services = new ServiceCollection();
            services.AddScoped<IScopedDependency, ScopedDependency>();
            services.AddFastMediator(scan => scan.FromAssemblyOf<AmbientScopeTests>());
            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<Dispatcher>();
            var dependencyInScope = scope.ServiceProvider.GetRequiredService<IScopedDependency>();

            var idSeenByHandler = dispatcher.Send(new ScopeProbeRequest());

            // L'handler condivide lo stesso scope (ambient): stessa istanza scoped.
            idSeenByHandler.Should().Be(dependencyInScope.Id);
        }

        [Fact]
        public void Send_DifferentScopes_GetDifferentScopedDependencies()
        {
            var services = new ServiceCollection();
            services.AddScoped<IScopedDependency, ScopedDependency>();
            services.AddFastMediator(scan => scan.FromAssemblyOf<AmbientScopeTests>());
            var provider = services.BuildServiceProvider();

            Guid first;
            Guid second;

            using (var scope = provider.CreateScope())
                first = scope.ServiceProvider.GetRequiredService<Dispatcher>().Send(new ScopeProbeRequest());

            using (var scope = provider.CreateScope())
                second = scope.ServiceProvider.GetRequiredService<Dispatcher>().Send(new ScopeProbeRequest());

            first.Should().NotBe(second);
        }

        [Fact]
        public void Send_WithValidationDisabled_DoesNotValidate()
        {
            var services = new ServiceCollection();
            services.AddFastMediator(scan => scan.FromAssemblyOf<ValidationTests>(), options => options.EnableValidation = false);
            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<Dispatcher>();

            // Richiesta non valida: senza validazione non deve lanciare.
            var response = dispatcher.Send(new ValidationTests.ValidatedRequest { Name = "", Age = -1 });

            response.Should().Be("Name: , Age: -1");
        }
    }
}
