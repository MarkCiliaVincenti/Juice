namespace Juice.EventBus
{
    public interface IEventBusSubscriptionsManager
    {
        bool IsEmpty { get; }

        event EventHandler<string> OnEventRemoved;

        void AddSubscription<T, TH>(string? key)
           where T : IntegrationEvent
           where TH : IIntegrationEventHandler<T>;

        void RemoveSubscription<T, TH>(string? key)
             where TH : IIntegrationEventHandler<T>
             where T : IntegrationEvent;

        bool HasSubscriptionsForEvent(string eventName);
        /// <summary>
        /// Return the registered event type by name
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        Type? GetEventTypeByName(string eventName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
        string GetDefaultEventKey(Type type);
    }

    public static class EventBusSubscriptionsManagerExtensions
    {
        public static string GetDefaultEventKey<T>(this IEventBusSubscriptionsManager subscriptionsManager)
            => subscriptionsManager.GetDefaultEventKey(typeof(T));

    }
}
