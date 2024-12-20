﻿using Juice.Modular;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Juice.Conventions.StartupDiscovery.Extensions
{
    public static class DiscoveredModulesMvcBuilderExtensions
    {
        /// <summary>
		/// Discover all <see cref="IModuleStartup"/> derived classes and regiester enabled modules
		/// </summary>
		/// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
		/// <returns>The <see cref="IMvcBuilder"/>.</returns>
		public static IMvcBuilder AddDiscoveredModules(this IMvcBuilder builder, IWebHostEnvironment env,
            IConfiguration configuration)
        {
            builder.PartManager.FeatureProviders.Add(new StartupDiscoveryFeatureProvider());
            var feature = new StartupDiscoveryFeature();
            builder.PartManager.PopulateFeature(feature);
            builder.Services.AddSingleton(feature);

            var serviceProvider = builder.Services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var nameProvider = serviceProvider.GetService<IFeatureNameProvider>();

            var logger = loggerFactory?.CreateLogger("Startup");

            EnableFeatures(builder, feature.Startups, configuration, logger, nameProvider);

            var startups = builder.Services.BuildServiceProvider().GetServices<IModuleStartup>()
                .OrderBy(s => s.StartOrder)
                .ToArray();
            var hasError = false;
            foreach (var startup in startups)
            {
                try
                {
                    startup.ConfigureServices(builder.Services, builder, env, configuration);
                }
                catch (Exception ex)
                {
                    hasError = true;
                    logger?.LogError("Failed to configure services {service}. Message: {message}", startup.GetType().FullName, ex.Message);
                    logger?.LogTrace(ex.StackTrace);
                }
            }

            if (hasError)
            {
                throw new Exception("Some module failed. Please enable logging for Startup at Trace level for more information.");
            }

            return builder;
        }

        private static void EnableFeatures(IMvcBuilder builder, IEnumerable<Type> types,
            IConfiguration configuration,
            ILogger? logger, IFeatureNameProvider? nameProvider)
        {
            var appOptions = new FeatureOptions();
            configuration.GetSection("App").Bind(appOptions);

            var enabled = appOptions.Features ?? Array.Empty<string>();
            var disabled = appOptions.ExcludedFeatures ?? Array.Empty<string>();

            var requirements = GetDependentFeatures(types, new HashSet<string>(), enabled, disabled, logger, nameProvider);

            var hasConflict = false;

            foreach (var startup in types)
            {
                var featureName = nameProvider?.GetFeatureName(startup) ?? startup.GetFeatureName();

                if (requirements.Any(f => f.Equals(featureName, StringComparison.OrdinalIgnoreCase)))
                {

                    var feature = startup.GetFeature();
                    if (feature != null && feature.IncompatibleFeatures != null && requirements.Any(f => feature.IncompatibleFeatures.Contains(f)))
                    {
                        var conflict = requirements.Any(f => feature.IncompatibleFeatures.Contains(f));
                        logger?.LogWarning($"Feature {featureName} is not compatible with {string.Join(", ", conflict)}");

                        hasConflict = true;
                    }


                    if (!builder.Services.Any(s => typeof(IModuleStartup).Equals(s.ServiceType)
                        && startup.Equals(s.ImplementationType))
                    )
                    {
                        logger?.LogInformation("Registered feature's startup {0} {1}", featureName, startup.FullName);
                        builder.Services.Add(new ServiceDescriptor(typeof(IModuleStartup), startup, ServiceLifetime.Transient));
                    }
                }
            }

            if (hasConflict)
            {
                throw new Exception("Some module conflicted. Please enable logging for Startup at Trace level for more information.");
            }
        }

        private static IEnumerable<string> GetDependentFeatures(IEnumerable<Type> types,
            HashSet<string> requirements, string[] enabled, string[] disabled,
            ILogger? logger, IFeatureNameProvider? nameProvider)
        {
            var hasNewRequirement = false;
            foreach (var type in types)
            {
                var featureName = nameProvider?.GetFeatureName(type) ?? type.GetFeatureName();
                var feature = type.GetFeature();

                if (
                    featureName != null &&
                    (enabled.Any(f => f.Equals(featureName, StringComparison.OrdinalIgnoreCase))
                    || requirements.Any(f => f.Equals(featureName, StringComparison.OrdinalIgnoreCase))
                    || feature != null && feature.Required
                    ) && !disabled.Any(f => f.Equals(featureName, StringComparison.OrdinalIgnoreCase))
                    )
                {
                    if (requirements.Add(featureName))
                    {
                        hasNewRequirement = true;
                        logger?.LogInformation("Added feature {0}", featureName);
                    }
                    if (feature != null && feature.Dependencies != null)
                    {
                        foreach (var requirement in feature.Dependencies)
                        {
                            if (!disabled.Any(f => f.Equals(requirement, StringComparison.OrdinalIgnoreCase))
                                && requirements.Add(requirement))
                            {
                                hasNewRequirement = true;
                                logger?.LogInformation("Added dependency feature {0}", requirement);
                            }
                        }
                    }
                }
            }
            if (hasNewRequirement)
            {
                return GetDependentFeatures(types, requirements, enabled, disabled, logger, nameProvider);
            }
            return requirements;
        }
    }
}
