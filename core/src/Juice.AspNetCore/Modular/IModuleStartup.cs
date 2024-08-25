﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juice.Modular
{
    public interface IModuleStartup
    {
        /// <summary>
        /// The order in which the module will be call ConfigureServices
        /// </summary>
        int StartOrder { get; }

        /// <summary>
        /// The order in which the module will be call Configure
        /// </summary>
        int ConfigureOrder { get; }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="mvc"></param>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        void ConfigureServices(IServiceCollection services, IMvcBuilder mvc, IWebHostEnvironment env, IConfiguration configuration);

        /// <summary>
        /// Configure pipeline
        /// </summary>
        /// <param name="app"></param>
        /// <param name="routes"></param>
        /// <param name="env"></param>
        void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IWebHostEnvironment env);

        /// <summary>
        /// Called when the application is shutting down
        /// </summary>
        /// <param name="env"></param>
        /// <param name="serviceProvider"></param>
        void OnShutdown(IServiceProvider serviceProvider, IWebHostEnvironment env);
    }

}
