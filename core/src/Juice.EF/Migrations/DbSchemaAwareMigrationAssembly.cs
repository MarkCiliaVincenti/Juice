﻿using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace Juice.EF.Migrations
{
#pragma warning disable EF1001 // Internal EF Core API usage.
    public class DbSchemaAwareMigrationAssembly : MigrationsAssembly
    {
        private readonly DbContext _context;

        public DbSchemaAwareMigrationAssembly(ICurrentDbContext currentContext,
              IDbContextOptions options, IMigrationsIdGenerator idGenerator,
              IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
          : base(currentContext, options, idGenerator, logger)
        {
            _context = currentContext.Context;
        }

        public override Migration CreateMigration(TypeInfo migrationClass,
              string activeProvider)
        {
            ArgumentNullException.ThrowIfNull(activeProvider);

            var hasCtorWithSchema = migrationClass
                    .GetConstructor(new[] { typeof(ISchemaDbContext) }) != null;

            if (hasCtorWithSchema && _context is ISchemaDbContext schema)
            {
                var instance = (Migration?)Activator.CreateInstance(migrationClass.AsType(), schema);
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Could not create an instance of {migrationClass.FullName}");
                }
                instance.ActiveProvider = activeProvider;
                return instance;
            }

            return base.CreateMigration(migrationClass, activeProvider);
        }
    }
#pragma warning restore EF1001 // Internal EF Core API usage.

}
