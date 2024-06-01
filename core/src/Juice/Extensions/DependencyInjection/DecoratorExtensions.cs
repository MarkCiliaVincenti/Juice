using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DecoratorExtensions
    {
        private static void DecorateInternal(this IServiceCollection services, Type serviceType, Type implementationType)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
            if (descriptor == null)
            {
                throw new InvalidOperationException($"Service of type {serviceType.FullName} not registered");
            }
            if(descriptor.ImplementationInstance != null)
            {
                services.Add(new ServiceDescriptor(serviceType, sp =>
                   ActivatorUtilities.CreateInstance(sp, implementationType, new object[] { descriptor.ImplementationInstance })
                   , descriptor.Lifetime
                ));
                services.Remove(descriptor);

            }
            else if(descriptor.ImplementationFactory != null)
            {
                services.Add(new ServiceDescriptor(serviceType, sp =>
                    ActivatorUtilities.CreateInstance(sp, implementationType, new object[] { descriptor.ImplementationFactory(sp) })
                    , descriptor.Lifetime
                ));
                services.Remove(descriptor);

            }
            else if(descriptor.ImplementationType != null)
            {
                services.Add(new ServiceDescriptor(serviceType, sp =>
                    ActivatorUtilities.CreateInstance(sp, implementationType, new object[] { ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType) })
                    , descriptor.Lifetime
                ));
                services.Remove(descriptor);

            }
        }

        public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type implementationType)
        {
            var descriptors = services.Where(d => d.ServiceType.IsAssignableTo(serviceType)).ToArray();
            foreach (var descriptor in descriptors)
            {
                services.DecorateInternal(descriptor.ServiceType, implementationType);
            }
            return services;
        }

        public static IServiceCollection Decorate<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.Decorate(typeof(TService), typeof(TImplementation));
        }
    }
}
