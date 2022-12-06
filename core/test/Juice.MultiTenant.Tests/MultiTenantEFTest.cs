﻿using System;
using System.Threading.Tasks;
using Finbuckle.MultiTenant;
using Juice.Extensions.Configuration;
using Juice.Extensions.DependencyInjection;
using Juice.Extensions.Options;
using Juice.Extensions.Options.Stores;
using Juice.MultiTenant.DependencyInjection;
using Juice.MultiTenant.EF;
using Juice.MultiTenant.EF.ConfigurationProviders.DependencyInjection;
using Juice.MultiTenant.EF.DependencyInjection;
using Juice.MultiTenant.EF.Migrations;
using Juice.Services;
using Juice.Tenants;
using Juice.XUnit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Juice.MultiTenant.Tests
{
    [TestCaseOrderer("Juice.XUnit.PriorityOrderer", "Juice.MultiTenant.Tests")]
    public class MultiTenantEFTest
    {
        private readonly ITestOutputHelper _output;

        public MultiTenantEFTest(ITestOutputHelper testOutput)
        {
            _output = testOutput;
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        }

        [IgnoreOnCITheory(DisplayName = "Migrations"), TestPriority(999)]
        [InlineData("SqlServer")]
        [InlineData("PostgreSQL")]
        public async Task TenantDbContext_should_migrate_Async(string provider)
        {
            var resolver = new DependencyResolver
            {
                CurrentDirectory = AppContext.BaseDirectory
            };

            resolver.ConfigureServices(services =>
            {
                var configService = services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
                var configuration = configService.GetConfiguration();

                // Register DbContext class

                services.AddDefaultStringIdGenerator();

                services.AddSingleton(provider => _output);

                services.AddLogging(builder =>
                {
                    builder.ClearProviders()
                    .AddTestOutputLogger()
                    .AddConfiguration(configuration.GetSection("Logging"));
                });

                services.AddTenantDbContext<Tenant>(configuration, options =>
                {
                    options.DatabaseProvider = provider;
                }, true);

                services.AddTenantSettingsDbContext(configuration, options =>
                {
                    options.DatabaseProvider = provider;
                });
            });

            var context = resolver.ServiceProvider.
                CreateScope().ServiceProvider.GetRequiredService<TenantStoreDbContextWrapper>();

            await context.MigrateAsync();
            await context.SeedAsync(resolver.ServiceProvider.GetRequiredService<IConfigurationService>()
                .GetConfiguration());

            var context1 = resolver.ServiceProvider.
                CreateScope().ServiceProvider.GetRequiredService<TenantSettingsDbContext>();
            await context1.MigrateAsync();
        }

        [IgnoreOnCITheory(DisplayName = "Read/write tenants configuration"), TestPriority(1)]
        [InlineData("SqlServer")]
        [InlineData("PostgreSQL")]
        public async Task Read_write_tenants_settings_Async(string provider)
        {
            using var host = Host.CreateDefaultBuilder()
                 .ConfigureAppConfiguration((hostContext, configApp) =>
                 {
                     configApp.Sources.Clear();
                     configApp.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                     configApp.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
                 })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Register DbContext class

                    services.AddDefaultStringIdGenerator();

                    services.AddSingleton(provider => _output);

                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders()
                        .AddTestOutputLogger()
                        .AddConfiguration(configuration.GetSection("Logging"));
                    });

                    services.AddScoped(sp =>
                    {
                        var id = DateTime.Now.Millisecond % 2 == 0 ? "TenantA" : "TenantB";
                        return new Tenant { Identifier = id, Id = id };
                    });

                    services.AddScoped<ITenant>(sp => sp.GetRequiredService<Tenant>());

                    services.AddScoped<ITenantInfo>(sp => sp.GetRequiredService<Tenant>());


                    services.AddTenantsConfiguration()
                        .AddTenantsJsonFile("appsettings.Development.json")
                        .AddTenantsEntityConfiguration(configuration, options =>
                        {
                            options.DatabaseProvider = provider;
                        });

                    services.UseTenantsOptionsMutableEFStore(configuration, options =>
                    {
                        options.DatabaseProvider = provider;
                    });

                    services.ConfigureTenantsOptionsMutable<Models.Options>("Options");

                }).Build();

            {
                using var scope = host.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TenantSettingsDbContext>();
                var store = scope.ServiceProvider.GetRequiredService<ITenantsOptionsMutableStore>();
            }

            for (var i = 0; i < 10; i++)
            {
                using var scope = host.Services.CreateScope();
                var options = scope.ServiceProvider
                    .GetRequiredService<ITenantsOptionsMutable<Models.Options>>();
                var time = DateTimeOffset.Now.ToString();
                _output.WriteLine(options.Value.Name + ": " + time);
                Assert.True(await options.UpdateAsync(o => o.Time = time));
                Assert.Equal(time, options.Value.Time);
            }

        }

    }
}