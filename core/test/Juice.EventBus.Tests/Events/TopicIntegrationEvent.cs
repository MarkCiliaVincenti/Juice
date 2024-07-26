using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juice.EventBus.Tests.Events
{
    public record TopicIntegrationEvent(string Key) : IntegrationEvent
    {
        public override string GetEventKey() => Key;
    }
}
