using System.Text.RegularExpressions;

namespace Juice.EventBus
{
    public static class RoutingKeyUtils
    {
        public static bool IsTopicMatch(string eventRoutingKey, string consumeRoutingKey)
        {
            return Regex.IsMatch(eventRoutingKey, ToRouteMatchingKey(consumeRoutingKey));
        }

        public static string ToRouteMatchingKey(string consumeRoutingKey)
        {
            return "^" + consumeRoutingKey.Replace(".", "\\.").Replace("*", "([^\\.]+){1}")
                 .Replace("\\.#", "(\\.[^\\.]+)*").Replace("#\\.", "([^\\.]+\\.)*") + "$";
        }
    }
}
