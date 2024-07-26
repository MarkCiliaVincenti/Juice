using Juice.EventBus;
using Juice.EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMQServiceCollectionExtensions
    {
        /// <summary>
        /// Register RabbitMQ Event Bus
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection RegisterRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration, Action<RabbitMQOptions>? configure = null)
        {
            var enabled = configuration.GetValue<bool>(nameof(RabbitMQOptions.RabbitMQEnabled));
            if (enabled)
            {
                services.Configure<RabbitMQOptions>(options =>
                {
                    configuration.Bind(options);
                    if (configure != null)
                    {
                        configure(options);
                    }
                });

                var options = new RabbitMQOptions();
                configuration.Bind(options);
                if (configure != null)
                {
                    configure(options);
                }

                services.AddSingleton<IEventBusSubscriptionsManager>(sp => {
                    var logger = sp.GetRequiredService<ILogger<InMemoryEventBusSubscriptionsManager>>();
                    return new InMemoryEventBusSubscriptionsManager(logger, options.ExchangeType == "topic");
                });

                services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();

                services.AddSingleton<IEventBus, RabbitMQEventBus>();

                services.AddIntegrationEventTypesService();
            }
            return services;

        }
    }
}
