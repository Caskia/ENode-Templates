﻿using BoundedContext.Api.Swagger;
using ENode.Configurations;
using Exceptionless;
using Jane.AspNetCore.Authentication;
using Jane.AspNetCore.Authentication.JwtBearer;
using Jane.AspNetCore.Cors;
using Jane.Configurations;
using Jane.Extensions;
using Jane.MongoDb.Serializers;
using Jane.Timing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Reflection;
using JaneConfiguration = Jane.Configurations.Configuration;

namespace BoundedContext.Api
{
    public class Startup
    {
        private Assembly[] _bussinessAssemblies;

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //Config Exceptionless
            if (env.IsProduction() && !JaneConfiguration.Instance.Root["Exceptionless:ApiKey"].IsNullOrEmpty())
            {
                app.UseExceptionless(JaneConfiguration.Instance.Root);
            }

            app.UseProcessingTimeHeader();

            app.UseHostNameHeader();

            app.UseJane();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseResponseCompression();

            app.UseJwtTokenMiddleware();

            app.UseMvcWithDefaultRoute();

            if (!env.IsProduction())
            {
                var rewriteOptions = new RewriteOptions()
                .AddRewrite("^docs/index", "docs/index.html", false)
                .AddRedirect("^$", "docs/index");

                app.UseRewriter(rewriteOptions);

                // Enable middleware to serve generated Swagger as a JSON endpoint
                app.UseSwagger(options => { options.SetupSwaggerOptions(); });

                // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
                app.UseSwaggerUI(options => { options.SetupSwaggerUIOptions(); });
            }
            else
            {
                app.MapWhen(context =>
                {
                    return context.Request.Path == "/";
                },
                application =>
                {
                    application.Run(async context =>
                    {
                        await context.Response.WriteAsync("ProjectName BoundedContext Api");
                    });
                });
            }
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //Configure ENode
            var enodeConfiguration = InitializeJane()
                .CreateECommon()
                .CreateENode(new ConfigurationSetting())
                .RegisterENodeComponents()
                .RegisterBusinessComponents(_bussinessAssemblies)
                .UseKafka();

            //Compression
            services.AddResponseCompression();

            //Configure CORS for application
            services.AddCorsPolicy(JaneConfiguration.Instance.Root);

            services.AddMvc(options =>
            {
                options.Filters.Add(new CorsAuthorizationFilterFactory(CorsPolicyNames.DefaultCorsPolicyName));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //Configure Auth
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .ConfigureJwtBearer(JaneConfiguration.Instance.Root);

            //Swagger
            services.AddSwaggerGen(options => { options.SetupSwaggerGenOptions(JaneConfiguration.Instance.Root); });

            //Configure ENode AspNetCore
            enodeConfiguration
                .UseENodeAspNetCore(services, out var serviceProvider)
                .InitializeBusinessAssemblies(_bussinessAssemblies)
                .StartKafka()
                .Start();

            JaneConfiguration.Instance.CreateAutoMapperMappings();

            return serviceProvider;
        }

        private JaneConfiguration InitializeJane()
        {
            _bussinessAssemblies = new[]
            {
                Assembly.Load("BoundedContext.Commands"),
                Assembly.Load("BoundedContext.Repositories.MongoDb"),
                Assembly.Load("BoundedContext.ReadModel"),
                Assembly.Load("BoundedContext.QueryServices"),
                Assembly.GetExecutingAssembly()
            };

            var autoMapperConfigurationAssemblies = new[]
            {
                Assembly.Load("BoundedContext.ReadModel"),
                Assembly.Load("BoundedContext.QueryServices"),
                Assembly.GetExecutingAssembly()
            };

            return JaneConfiguration.Create()
                 .UseAutofac()
                 .RegisterCommonComponents()
                 .RegisterAssemblies(_bussinessAssemblies)
                 .UseAspNetCore()
                 .UseLog4Net()
                 .UseClockProvider(ClockProviders.Utc)
                 .UseAutoMapper(autoMapperConfigurationAssemblies)
                 .UseMongoDb(() =>
                    {
                        var pack = new ConventionPack
                        {
                            new EnumRepresentationConvention(BsonType.String)
                        };
                        ConventionRegistry.Register("EnumStringConvention", pack, t => true);
                        BsonSerializer.RegisterSerializer(typeof(Dictionary<string, object>), new ObjectDictionarySerializer());
                        BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                        BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
                    })
                 .UseRedis()
                 .LoadKafkaConfiguration()
                 .LoadENodeConfiguration()
                 .RegisterUnhandledExceptionHandler();
        }
    }
}