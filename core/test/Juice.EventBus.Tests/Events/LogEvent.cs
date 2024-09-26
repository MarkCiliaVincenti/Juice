using Microsoft.Extensions.Logging;

namespace Juice.EventBus.Tests.Events
{
    public record LogEvent : IntegrationEvent
    {
        public LogLevel Serverty { get; set; }
        public required string Facility { get; set; }

        public override string GetEventKey() => (Facility + "." + Serverty).ToLower();
    }

}
