using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Juice.MediatR;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentifiedCommandServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentifiedCommandHandler<TRequest, THandler, TIdentifiedHandler>(this IServiceCollection services)
            where TRequest : IRequest
            where THandler : class, IRequestHandler<TRequest>
            where TIdentifiedHandler : class, IRequestHandler<IdentifiedCommand<TRequest>>
        {
            services.AddTransient<IRequestHandler<TRequest>, THandler>();
            services.AddTransient<IRequestHandler<IdentifiedCommand<TRequest>>, TIdentifiedHandler>();
            return services;
        }

        public static IServiceCollection AddIdentifiedCommandHandler<TRequest, TResponse, THandler, TIdentifiedHandler>(this IServiceCollection services)
            where TRequest : IRequest<TResponse>
            where THandler : class, IRequestHandler<TRequest, TResponse>
            where TIdentifiedHandler : class, IRequestHandler<IdentifiedCommand<TRequest, TResponse>, TResponse>
        {
            services.AddTransient<IRequestHandler<TRequest, TResponse>, THandler>();
            services.AddTransient<IRequestHandler<IdentifiedCommand<TRequest, TResponse>, TResponse>, TIdentifiedHandler>();
            return services;
        }
    }
}
