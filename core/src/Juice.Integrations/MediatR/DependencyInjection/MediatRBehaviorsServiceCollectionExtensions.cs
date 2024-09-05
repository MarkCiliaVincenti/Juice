﻿using Juice.Integrations.MediatR.Behaviors;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MediatRBehaviorsServiceCollectionExtensions
    {
        public static IServiceCollection AddOperationExceptionBehavior(this IServiceCollection services)
        {
            return services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OperationExceptionBehavior<,>));
        }
    }
}
