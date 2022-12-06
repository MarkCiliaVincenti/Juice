﻿using Finbuckle.MultiTenant;
using Juice.Domain;
using Juice.EF.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juice.MultiTenant.EF.DependencyInjection
{
    public static class TenantDbServiceCollectionExtensions
    {
        /// <summary>
        /// Add TenantDbContext with db options
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="dbOptions"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static IServiceCollection AddTenantDbContext<TTenantInfo>(this IServiceCollection services, IConfiguration configuration, Juice.EF.DbOptions dbOptions, bool migrate)
             where TTenantInfo : class, IAuditable, IDynamic, ITenantInfo, new()
        {
            services.AddScoped(sp =>
                new Juice.EF.DbOptions<TenantStoreDbContext<TTenantInfo>> { Schema = dbOptions.Schema, ConnectionName = dbOptions.ConnectionName, DatabaseProvider = dbOptions.DatabaseProvider });

            var provider = dbOptions.DatabaseProvider ?? "SqlServer";

            var connectionName = dbOptions.ConnectionName ??
                provider switch
                {
                    "PostgreSQL" => "PostgreConnection",
                    "SqlServer" => "SqlServerConnection",
                    _ => throw new NotSupportedException($"Unsupported provider: {provider}")
                }
                ;
            var connectionString = configuration.GetConnectionString(connectionName);

            Action<DbContextOptionsBuilder> configure = (options) =>
            {
                switch (provider)
                {
                    case "PostgreSQL":
                        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

                        options.UseNpgsql(
                           connectionString,
                            x =>
                            {
                                x.MigrationsHistoryTable("__EFTenantMigrationsHistory", dbOptions.Schema ?? "App");
                                x.MigrationsAssembly("Juice.MultiTenant.EF.PostgreSQL");
                            });
                        break;

                    case "SqlServer":

                        options.UseSqlServer(
                            connectionString,
                        x =>
                        {
                            x.MigrationsHistoryTable("__EFTenantMigrationsHistory", dbOptions.Schema ?? "App");
                            x.MigrationsAssembly("Juice.MultiTenant.EF.SqlServer");
                        });
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported provider: {provider}");
                }

                options
                    .ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>()
                ;
            };

            services.AddDbContext<TenantStoreDbContext<TTenantInfo>>(configure);

            // for migration only
            if (migrate)
            {
                services.AddDbContext<TenantStoreDbContextWrapper>(
                   configure);
            }

            return services;
        }

        /// <summary>
        /// Add TenantDbContext with configure db options action
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddTenantDbContext<TTenantInfo>(this IServiceCollection services, IConfiguration configuration, Action<Juice.EF.DbOptions> configureOptions, bool migrate)
            where TTenantInfo : class, IAuditable, IDynamic, ITenantInfo, new()
        {
            var options = new Juice.EF.DbOptions<TenantStoreDbContext<TTenantInfo>>();
            configureOptions(options);
            return services.AddTenantDbContext<TTenantInfo>(configuration, options, migrate);
        }

        /// <summary>
        /// Add TenantSettingsDbContext with db options
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="dbOptions"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static IServiceCollection AddTenantSettingsDbContext(this IServiceCollection services, IConfiguration configuration, Juice.EF.DbOptions dbOptions)
        {
            services.AddScoped(sp =>
                new Juice.EF.DbOptions<TenantSettingsDbContext> { Schema = dbOptions.Schema, ConnectionName = dbOptions.ConnectionName, DatabaseProvider = dbOptions.DatabaseProvider });

            var provider = dbOptions.DatabaseProvider ?? "SqlServer";

            var connectionName = dbOptions.ConnectionName ??
                provider switch
                {
                    "PostgreSQL" => "PostgreConnection",
                    "SqlServer" => "SqlServerConnection",
                    _ => throw new NotSupportedException($"Unsupported provider: {provider}")
                }
                ;
            var connectionString = configuration.GetConnectionString(connectionName);
            services.AddDbContext<TenantSettingsDbContext>(
               options =>
               {
                   switch (provider)
                   {
                       case "PostgreSQL":
                           AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

                           options.UseNpgsql(
                              connectionString,
                               x =>
                               {
                                   x.MigrationsHistoryTable("__EFTenantSettingsMigrationsHistory", dbOptions.Schema ?? "App");
                                   x.MigrationsAssembly("Juice.MultiTenant.EF.PostgreSQL");
                               });
                           break;

                       case "SqlServer":

                           options.UseSqlServer(
                               connectionString,
                           x =>
                           {
                               x.MigrationsHistoryTable("__EFTenantSettingsMigrationsHistory", dbOptions.Schema ?? "App");
                               x.MigrationsAssembly("Juice.MultiTenant.EF.SqlServer");
                           });
                           break;
                       default:
                           throw new NotSupportedException($"Unsupported provider: {provider}");
                   }

                   options
                       .ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>()
                   ;
               });

            return services;
        }

        /// <summary>
        /// Add TenantSettingsDbContext with configure db options action
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddTenantSettingsDbContext(this IServiceCollection services, IConfiguration configuration, Action<Juice.EF.DbOptions> configureOptions)
        {
            var options = new Juice.EF.DbOptions<TenantSettingsDbContext>();
            configureOptions(options);
            return services.AddTenantSettingsDbContext(configuration, options);
        }
    }
}