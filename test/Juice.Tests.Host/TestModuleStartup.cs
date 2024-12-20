﻿using Juice.EventBus;
using Juice.Modular;
using Juice.Tests.Host.IntegrationEvents;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace Juice.Tests.Host
{
    [Feature(Required = true)]
    public class TestModuleStartup : ModuleStartup
    {
        public override void ConfigureServices(IServiceCollection services, IMvcBuilder mvc, IWebHostEnvironment env, IConfiguration configuration)
        {
            services.ConfigureTenantsOptions<Options>("Options");


            services.AddMemoryCache();

            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(() =>
                {
                    var options = new ConfigurationOptions();
                    configuration.GetSection("Redis:ConfigurationOptions").Bind(options);
                    var endpoints = configuration.GetSection("Redis:ConfigurationOptions:EndPoints")?.Get<string[]>() ?? Array.Empty<string>();
                    foreach (var endpoint in endpoints)
                    {
                        options.EndPoints.Add(endpoint);
                    }
                    var redis = ConnectionMultiplexer.Connect(options);

                    return redis.GetDatabase();

                }, "DataProtection-Keys");

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "SampleInstance";
            });

            services.AddTransient<TenantActivatedIntegrationEventHandler>();
            services.AddTransient<TenantSettingsChangedIntegrationEventHandler>();
            services.AddTransient<LogEventHandler>();

            services.RegisterRabbitMQEventBus(configuration.GetSection("RabbitMQ"),
                 options =>
                 {
                     options.BrokerName = "topic.juice_bus";
                     options.SubscriptionClientName = "juice_test_host_events";
                     options.ExchangeType = "topic";
                 });

        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IWebHostEnvironment env)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            eventBus.Subscribe<TenantActivatedIntegrationEvent, TenantActivatedIntegrationEventHandler>();
            eventBus.Subscribe<TenantSettingsChangedIntegrationEvent, TenantSettingsChangedIntegrationEventHandler>();
            eventBus.Subscribe<LogEvent, LogEventHandler>("kernel.*");
        }

        public override void OnShutdown(IServiceProvider serviceProvider, IWebHostEnvironment env)
        {
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();

            eventBus.Unsubscribe<TenantActivatedIntegrationEvent, TenantActivatedIntegrationEventHandler>();
            eventBus.Unsubscribe<TenantSettingsChangedIntegrationEvent, TenantSettingsChangedIntegrationEventHandler>();
            eventBus.Unsubscribe<LogEvent, LogEventHandler>("kernel.*");
        }
    }
}
