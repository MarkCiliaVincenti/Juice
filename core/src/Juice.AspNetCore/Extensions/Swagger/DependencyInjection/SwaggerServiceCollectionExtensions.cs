﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juice.Extensions.Swagger
{
    public static class SwaggerServiceCollectionExtensions
    {
        [Obsolete("Use RequiredScope attibute to specify scope required by api")]
        public static IServiceCollection ConfigureSwaggerApiOptions(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ApiOptions>(configuration);
            return services;
        }
        public static IServiceCollection AddSwaggerWithDefaultConfigs(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                c.IgnoreObsoleteActions();

                c.IgnoreObsoleteProperties();

                c.SchemaFilter<SwaggerIgnoreFilter>();

                c.UseInlineDefinitionsForEnums();
            });

            services.AddSwaggerGenNewtonsoftSupport();

            return services;
        }
    }
}
