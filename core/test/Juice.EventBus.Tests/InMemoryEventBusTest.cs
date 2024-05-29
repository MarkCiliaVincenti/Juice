using System;
using System.Threading.Tasks;
using FluentAssertions;
using Juice.EventBus.Tests.Events;
using Juice.EventBus.Tests.Handlers;
using Juice.Extensions.DependencyInjection;
using Juice.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Juice.EventBus.Tests
{
    public class InMemoryEventBusTest
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public InMemoryEventBusTest(ITestOutputHelper testOutput)
        {

            var resolver = new DependencyResolver
            {
                CurrentDirectory = AppContext.BaseDirectory
            };

            resolver.ConfigureServices(services =>
            {
                var configService = services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
                var configuration = configService.GetConfiguration();

                services.AddSingleton(provider => testOutput);
                services.AddSingleton<HandledService>();

                services.AddLogging(builder =>
                {
                    builder.ClearProviders()
                    .AddTestOutputLogger()
                    .AddConfiguration(configuration.GetSection("Logging"));
                });

                services.RegisterInMemoryEventBus();

                services.AddTransient<ContentPublishedIntegrationEventHandler>();
                services.AddTransient<ContentPublishedIntegrationEventHandler1>();

                services.AddSingleton<HandledService>();

                services.AddScoped<ScopedService>();
            });

            _serviceProvider = resolver.ServiceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<InMemoryEventBusTest>>();
        }

        [IgnoreOnCIFact(DisplayName = "IntegrationEvent with InMemory event bus")]
        public async Task InMemoryTestAsync()
        {
            var eventBus = _serviceProvider.GetService<IEventBus>();
            if (eventBus != null)
            {
                var handledService = _serviceProvider.GetRequiredService<HandledService>();

                eventBus.Subscribe<ContentPublishedIntegrationEvent, ContentPublishedIntegrationEventHandler>();
                eventBus.Subscribe<ContentPublishedIntegrationEvent, ContentPublishedIntegrationEventHandler1>();

                await eventBus.PublishAsync(new ContentPublishedIntegrationEvent("Hello"));
                handledService.Handlers.Should().BeEmpty();
                await Task.Delay(TimeSpan.FromSeconds(5));
                handledService.Handlers.Should().Contain(nameof(ContentPublishedIntegrationEventHandler));
                handledService.Handlers.Should().Contain(nameof(ContentPublishedIntegrationEventHandler1));
            }
        }
    }
}
