using Juice.EventBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusServiceCollectionExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="topicSupport"></param>
        /// <returns></returns>
        public static IServiceCollection RegisterInMemoryEventBus(this IServiceCollection services, bool topicSupport = true)
        {
            services.AddIntegrationEventTypesService();

            services.AddSingleton<IEventBusSubscriptionsManager>(sp => {
                var logger = sp.GetRequiredService<ILogger<InMemoryEventBusSubscriptionsManager>>();
                return new InMemoryEventBusSubscriptionsManager(logger, topicSupport);
            });

            services.AddSingleton<IEventBus, InMemoryEventBus>();

            return services;
        }

        public static IServiceCollection AddIntegrationEventTypesService(this IServiceCollection services)
        {
            services.TryAddSingleton<IntegrationEventTypes>();
            return services;
        }
    }
}
