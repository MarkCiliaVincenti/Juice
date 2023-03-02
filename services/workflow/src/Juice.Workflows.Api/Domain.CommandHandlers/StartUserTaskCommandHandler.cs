﻿using Juice.EventBus;
using Juice.Workflows.Domain.AggregatesModel.EventAggregate;
using Juice.Workflows.Nodes.Activities;

namespace Juice.Workflows.Api.Domain.CommandHandlers
{
    public class StartUserTaskCommandHandler : StartTaskCommandHandler<UserTask>
    {
        public StartUserTaskCommandHandler(IEventBus eventBus, IEventRepository eventRepository) : base(eventBus, eventRepository)
        {

        }
    }
}
