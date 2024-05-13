using System.Threading.Tasks;
using Juice.EventBus.Tests.Events;
using Microsoft.Extensions.Logging;

namespace Juice.EventBus.Tests.Handlers
{
    public class ContentPublishedIntegrationEventHandler : IIntegrationEventHandler<ContentPublishedIntegrationEvent>
    {
        private ILogger _logger;
        private readonly HandledService _handledService;
        public ContentPublishedIntegrationEventHandler(ILogger<ContentPublishedIntegrationEventHandler> logger, HandledService handledService)
        {
            _logger = logger;
            _handledService = handledService;
        }
        public async Task HandleAsync(ContentPublishedIntegrationEvent @event)
        {
            await Task.Delay(200);
            _logger.LogInformation("[X] Received {0} at {1}", @event.Message, @event.CreationDate);
            _handledService.Handlers.Add(nameof(ContentPublishedIntegrationEventHandler));
        }
    }
}
