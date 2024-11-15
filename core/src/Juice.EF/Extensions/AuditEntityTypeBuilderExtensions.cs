﻿using Juice.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Juice.EF.Extensions
{
    public static class AuditEntityTypeBuilderExtensions
    {

        public static bool IsAuditable(this IMutableEntityType? entityType)
        {
            if (entityType?.ClrType?.IsAssignableTo(typeof(IAuditable)) ?? false)
            {
                return true;
            }
            while (entityType != null)
            {
                var hasAnnotation = (bool?)entityType.FindAnnotation(Constants.AuditAnnotationName)?.Value ?? false;
                if (hasAnnotation)
                {
                    return true;
                }
                entityType = entityType.BaseType;
            }

            return false;
        }

        public static bool IsAuditable(this IEntityType? entityType)
        {
            if (entityType?.ClrType?.IsAssignableTo(typeof(IAuditable)) ?? false)
            {
                return true;
            }
            while (entityType != null)
            {
                var hasAnnotation = (bool?)entityType.FindAnnotation(Constants.AuditAnnotationName)?.Value ?? false;
                if (hasAnnotation)
                {
                    return true;
                }
                entityType = entityType.BaseType;
            }

            return false;
        }

        public static EntityTypeBuilder IsAuditable(this EntityTypeBuilder builder)
        {
            try
            {
                if (builder.Metadata.ClrType.IsAssignableTo(typeof(ICreationInfo)))
                {
                    builder.Property<string?>(nameof(ICreationInfo.CreatedUser)).HasMaxLength(Constants.NameLength);
                }
                if (builder.Metadata.ClrType.IsAssignableTo(typeof(IModificationInfo)))
                {
                    builder.Property<string?>(nameof(IModificationInfo.ModifiedUser)).HasMaxLength(Constants.NameLength);
                }
                builder.HasAnnotation(Constants.AuditAnnotationName, true);
            }
            catch (Exception ex)
            {
                throw new Exception($"{builder.Metadata.ClrType} unable to add Audit properties. " + ex.Message, ex);
            }

            return builder;
        }


        /// <summary>
        /// Mark all entities that implemented IAuditable interface IsAuditable 
        /// </summary>
        /// <param name="modelBuilder"></param>
        /// <returns></returns>
        public static ModelBuilder ConfigureAuditableEntities(this ModelBuilder modelBuilder)
        {
            // Call IsAuditable() to configure the types marked with the AuditAnnotation
            foreach (var clrType in modelBuilder.Model.GetEntityTypes()
                                                 .Where(et => et.ClrType.IsAssignableTo(typeof(IAuditable)))
                                                 .Select(et => et.ClrType))
            {
                modelBuilder.Entity(clrType)
                            .IsAuditable();
            }

            return modelBuilder;
        }
    }
}
