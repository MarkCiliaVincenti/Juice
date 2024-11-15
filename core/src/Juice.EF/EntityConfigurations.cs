﻿using Juice.Domain;
using Juice.EF.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Juice.EF
{
    public class EntityConfiguration<T, TKey> : IEntityTypeConfiguration<T>
        where T : class, IIdentifiable<TKey>
        where TKey : IEquatable<TKey>
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            if (builder.Property("Name")!=null)
            {
                builder.Property("Name").HasMaxLength(Constants.NameLength).IsRequired();
            }
            if (builder.Property("Disabled") != null)
            {
                builder.Property("Disabled").HasDefaultValue(false);
            }
        }
    }

    public class AuditEntityConfiguration<T, TKey> : EntityConfiguration<T, TKey>
        where T : class, IIdentifiable<TKey>, IAuditable
        where TKey : IEquatable<TKey>
    {
        public override void Configure(EntityTypeBuilder<T> builder)
        {
            base.Configure(builder);

            builder.IsAuditable();
        }
    }

    //public class DynamicEntityConfiguration<T, TKey> : EntityConfiguration<T, TKey>, IEntityTypeConfiguration<T>
    //    where T : class, IDynamic, IAuditable, IIdentifiable<TKey>
    //    where TKey : IEquatable<TKey>
    //{
    //    public override void Configure(EntityTypeBuilder<T> builder)
    //    {
    //        base.Configure(builder);

    //        builder.MarkAsDynamicExpandable();
    //        builder.MarkAsAuditable();
    //    }
    //}

    //public class DynamicConfiguration<T, TKey> : IEntityTypeConfiguration<T>
    //    where T : class, IDynamic
    //    where TKey : IEquatable<TKey>
    //{
    //    public virtual void Configure(EntityTypeBuilder<T> builder)
    //    {
    //        builder.MarkAsDynamicExpandable();
    //    }
    //}

}
