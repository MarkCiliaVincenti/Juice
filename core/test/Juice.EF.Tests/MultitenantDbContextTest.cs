using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Internal;
using FluentAssertions;
using Juice.EF.Tests.Infrastructure;
using Juice.Extensions.DependencyInjection;
using Juice.XUnit;
using Juice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Juice.EF.Tests
{
    public class MultitenantDbContextTest
    {
        private readonly ITestOutputHelper _output;

        public MultitenantDbContextTest(ITestOutputHelper testOutput)
        {
            _output = testOutput;
        }

        [IgnoreOnCIFact(DisplayName = "Multitenant entity"), TestPriority(1)]
        public async Task Multitenant_dbcontext_shoudAsync()
        {
            var resolver = new DependencyResolver
            {
                CurrentDirectory = AppContext.BaseDirectory
            };

            resolver.ConfigureServices(services =>
            {
                var configService = services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
                var configuration = configService.GetConfiguration();

                services.AddSingleton<SharedService>();
                var connectionString = configService.GetConfiguration().GetConnectionString("Default");
                // Register DbContext class
                services.AddDbContext<TestContext>(options =>
                {
                    options.UseSqlServer(connectionString, options =>
                    {
                        options.MigrationsHistoryTable("__EFTestMigrationsHistory", "Contents");
                    });
                });
                services
                    .AddMultiTenant<TenantInfo>()
                    .WithInMemoryStore(options =>
                    {
                        options.Tenants.Add(new TenantInfo { Id = "tenant-1", Identifier = "tenant-1" });
                    })
                    .WithStaticStrategy("tenant-1");
                ;
                services.AddMediatR(options =>
                {
                    options.RegisterServicesFromAssemblyContaining(typeof(EFTest));
                });

                services.AddSingleton(provider => _output);

                services.AddLogging(builder =>
                {
                    builder.ClearProviders()
                    .AddTestOutputLogger()
                    .AddConfiguration(configuration.GetSection("Logging"));
                });

            });

            var serviceProvider = resolver.ServiceProvider.CreateScope().ServiceProvider;
            HttpContext httpContext = new MyHttpContext(serviceProvider);
            var next = new RequestDelegate(async context =>
            {
                var tenantContextAccessor = serviceProvider.GetRequiredService<IMultiTenantContextAccessor>();
                tenantContextAccessor.MultiTenantContext.Should().NotBeNull();
                tenantContextAccessor.MultiTenantContext.TenantInfo.Should().NotBeNull();
                var dbContext = serviceProvider.GetRequiredService<TestContext>();
                dbContext.TenantInfo.Should().NotBeNull();
                _output.WriteLine("dbContext.TenantInfo: {0}", dbContext.TenantInfo.Identifier);
            });
            await (new MultiTenantMiddleware(next).Invoke(httpContext));
            
        }

    }
    internal class MyHttpContext : HttpContext
    {
        private readonly IServiceProvider _serviceProvider;

        public MyHttpContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override IFeatureCollection Features => throw new NotImplementedException();

        public override HttpRequest Request => throw new NotImplementedException();

        public override HttpResponse Response => throw new NotImplementedException();

        public override ConnectionInfo Connection => throw new NotImplementedException();

        public override WebSocketManager WebSockets => throw new NotImplementedException();

        public override ClaimsPrincipal User { get; set; }
        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();
        public override IServiceProvider RequestServices { get => _serviceProvider; set => throw new NotImplementedException(); }
        public override CancellationToken RequestAborted { get; set; }
        public override string TraceIdentifier { get; set; } = new DefaultStringIdGenerator().GenerateUniqueId();
        public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Abort() { }
}
}
