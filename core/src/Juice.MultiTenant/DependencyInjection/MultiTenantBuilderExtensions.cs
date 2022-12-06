﻿using Finbuckle.MultiTenant;
using Juice.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace Juice.MultiTenant.DependencyInjection
{
    public static class MultiTenantBuilderExtensions
    {
        public static FinbuckleMultiTenantBuilder<TTenantInfo> JuiceIntegration<TTenantInfo>(this FinbuckleMultiTenantBuilder<TTenantInfo> builder)
            where TTenantInfo : class, ITenantInfo, new()
        {
            builder.Services.AddScoped<ITenant>(sp => sp.GetService<Tenant>()!);

            return builder;
        }
    }
}