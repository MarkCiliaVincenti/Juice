﻿using System.Threading.Tasks;
using Juice.EventBus.Tests.Events;
using Microsoft.Extensions.Logging;

namespace Juice.EventBus.Tests.Handlers
{
    public class ContentPublishedIntegrationEventHandler : IIntegrationEventHandler<ContentPublishedIntegrationEvent>
    {
        private ILogger _logger;
        private readonly HandledService _handledService;
        private readonly ScopedService? _scopedService;
        public ContentPublishedIntegrationEventHandler(ILogger<ContentPublishedIntegrationEventHandler> logger, HandledService handledService, ScopedService? scopedService = default)
        {
            _logger = logger;
            _handledService = handledService;
            _scopedService = scopedService;
        }
        public async Task HandleAsync(ContentPublishedIntegrationEvent @event)
        {
            await Task.Delay(200);
            _logger.LogInformation("[X] Received {0} at {1}", @event.Message, @event.CreationDate);
            if (_scopedService == null || !_scopedService.IsDisposed)
            {
                _handledService.Handlers.Add(nameof(ContentPublishedIntegrationEventHandler));
            }
        }
    }
}
