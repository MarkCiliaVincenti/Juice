﻿// <auto-generated />
using System;
using Juice.MultiTenant.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Juice.MultiTenant.EF.PostgreSQL.Migrations.TenantSettings
{
    [DbContext(typeof(TenantSettingsDbContext))]
    [Migration("20230607072651_ChangeValueLength")]
    partial class ChangeValueLength
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Juice.MultiTenant.Domain.AggregatesModel.SettingsAggregate.TenantSettings", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<string>("TenantId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Value")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("Id");

                    b.HasIndex("Key", "TenantId")
                        .IsUnique();

                    b.ToTable("TenantSettings", "App");

                    b.HasAnnotation("Finbuckle:MultiTenant", true);
                });
#pragma warning restore 612, 618
        }
    }
}