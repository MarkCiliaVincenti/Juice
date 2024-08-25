using System;
using System.Threading.Tasks;
using FluentAssertions;
using Juice.EventBus.RabbitMQ;
using Juice.EventBus.Tests.Events;
using Juice.EventBus.Tests.Handlers;
using Juice.Extensions.DependencyInjection;
using Juice.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Juice.EventBus.Tests
{
    public class RabbitMQEventBusTest
    {
        private readonly ITestOutputHelper _output;

        public RabbitMQEventBusTest(ITestOutputHelper testOutput)
        {
            _output = testOutput;
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        }


        [IgnoreOnCIFact(DisplayName = "Integration Event with RabbitMQ")]
        public async Task IntegrationEventTestAsync()
        {
            var resolver = new DependencyResolver
            {
                CurrentDirectory = AppContext.BaseDirectory
            };

            resolver.ConfigureServices(services =>
            {

                var configService = services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
                var configuration = configService.GetConfiguration();

                services.AddSingleton(_output);

                services.AddLogging(builder =>
                {
                    builder.ClearProviders()
                    .AddTestOutputLogger()
                    .AddConfiguration(configuration.GetSection("Logging"));
                });

                services.AddHttpContextAccessor();

                services.RegisterRabbitMQEventBus(configuration.GetSection("RabbitMQ"));

                services.AddScoped<ScopedService>();

                services.AddTransient<ContentPublishedIntegrationEventHandler>();
                services.AddTransient<ContentPublishedIntegrationEventHandler1>();
                services.AddSingleton<HandledService>();
            });

            var serviceProvider = resolver.ServiceProvider;
            var eventBus = serviceProvider.GetService<IEventBus>();
            var handledService = serviceProvider.GetRequiredService<HandledService>();
            if (eventBus != null)
            {
                eventBus.Subscribe<ContentPublishedIntegrationEvent, ContentPublishedIntegrationEventHandler>();
                eventBus.Subscribe<ContentPublishedIntegrationEvent, ContentPublishedIntegrationEventHandler1>();

                await Task.Delay(TimeSpan.FromSeconds(3)); // wait for pending messages to be processed
                handledService.Handlers.Clear();

                for (var i = 0; i < 10; i++)
                {
                    await eventBus.PublishAsync(new ContentPublishedIntegrationEvent($"Hello {i}"));
                }

                await Task.Delay(TimeSpan.FromSeconds(5));

                eventBus.Unsubscribe<ContentPublishedIntegrationEvent, ContentPublishedIntegrationEventHandler>();
                eventBus.Unsubscribe<ContentPublishedIntegrationEvent, ContentPublishedIntegrationEventHandler1>();
                await Task.Delay(TimeSpan.FromSeconds(1));

                handledService.Handlers.Should().HaveCount(20);
            }
        }


        [IgnoreOnCIFact(DisplayName = "Send topic event"), TestPriority(800)]
        public async Task Send_topic_event_Async()
        {
            _output.WriteLine("THIS TEST RUN WITH Juice.Tests.Host TOGETHER");
            var resolver = new DependencyResolver
            {
                CurrentDirectory = AppContext.BaseDirectory
            };

            resolver.ConfigureServices(services =>
            {
                var configService = services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
                var configuration = configService.GetConfiguration();

                services.AddSingleton(provider => _output);

                services.AddLogging(builder =>
                {
                    builder.ClearProviders()
                    .AddTestOutputLogger()
                    .AddConfiguration(configuration.GetSection("Logging"));
                });


                services.RegisterRabbitMQEventBus(configuration.GetSection("RabbitMQ"),
                    options =>
                    {
                        options.BrokerName = "topic.juice_bus";
                        options.SubscriptionClientName = "juice_eventbus_test_events";
                        options.ExchangeType = "topic";
                    });

                services.AddSingleton<HandledService>();
                services.AddTransient<LogEventHandler>();
            });

            using var scope = resolver.ServiceProvider.CreateScope();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var handledService = scope.ServiceProvider.GetRequiredService<HandledService>();

            eventBus.Subscribe<LogEvent, LogEventHandler>("kernel.*");

            await eventBus.PublishAsync(new LogEvent { Facility = "auth", Serverty = LogLevel.Error });
            await Task.Delay(TimeSpan.FromSeconds(1));

            handledService.Handlers.Should().BeEmpty();

            await eventBus.PublishAsync(new LogEvent { Facility = "kernel", Serverty = LogLevel.Error });
            await eventBus.PublishAsync(new LogEvent { Facility = "kernel", Serverty = LogLevel.Information });

            await Task.Delay(TimeSpan.FromSeconds(1));
            handledService.Handlers.Should().HaveCount(2);
        }

    }

}
