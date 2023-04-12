﻿using Juice.BgService.Management.File;
using Juice.Extensions.Options.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Juice.BgService.Api.Extensions
{
    public static class BgWebApplicationExtensions
    {
        public static void UseBgServiceSwaggerUI(this WebApplication app)
        {
            app.UseSwagger(options => options.RouteTemplate = "bgservice/swagger/{documentName}/swagger.json");
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("bgservice-v1/swagger.json", "Background Service API V1");
                c.RoutePrefix = "bgservice/swagger";
            });
        }


        public static void SeparateStoreFile(this WebApplicationBuilder builder, string name)
        {
            builder.Services.UseOptionsMutableFileStore<FileStoreOptions>($"appsettings.{name}.{builder.Environment.EnvironmentName}.json");

            builder.Host.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile($"appsettings.{name}.{context.HostingEnvironment.EnvironmentName}.json");
            });
        }
    }
}
