using Autofac;
using BoundedContext.Api.Swagger;
using ENode.Configurations;
using Exceptionless;
using Jane.AspNetCore.Authentication;
using Jane.AspNetCore.Authentication.JwtBearer;
using Jane.AspNetCore.Cors;
using Jane.AspNetCore.Mvc;
using Jane.Configurations;
using Jane.Extensions;
using Jane.MongoDb.Serializers;
using Jane.Timing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
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
                app.UseExceptionless();
            }

            app.UseProcessingTimeHeader();

            app.UseHostNameHeader();

            app.UseJaneENode();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseResponseCompression();

            app.UseJwtTokenMiddleware();

            app.UseRouting();

            app.UseCors(CorsPolicyNames.DefaultCorsPolicyName);

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

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

            JaneConfiguration.Instance.CreateAutoMapperMappings();

            ENodeConfiguration.Instance
                    .InitializeBusinessAssemblies(_bussinessAssemblies)
                    .StartKafka()
                    .Start();
        }

        public void ConfigureContainer(ContainerBuilder builder)
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

            JaneConfiguration.Instance
                    .UseAutofac(builder)
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
                    .RegisterUnhandledExceptionHandler()
                    .CreateECommon(builder)
                    .CreateENode(new ConfigurationSetting())
                    .RegisterENodeComponents()
                    .RegisterBusinessComponents(_bussinessAssemblies)
                    .UseKafka();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //Configure Jane
            JaneConfiguration.Create();

            //Config Exceptionless
            if (!JaneConfiguration.Instance.Root["Exceptionless:ApiKey"].IsNullOrEmpty())
            {
                services.AddExceptionless(config => config.ReadFromConfiguration(JaneConfiguration.Instance.Root));
            }

            services.AddJaneAspNetCore();

            //Compression
            services.AddResponseCompression();

            //Configure CORS for application
            services.AddCorsPolicy(JaneConfiguration.Instance.Root);

            services.AddControllers()
                .AddJaneJsonOptions();

            //Configure Auth
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .ConfigureJwtBearer(JaneConfiguration.Instance.Root);

            //Swagger
            services.AddSwaggerGen(options => { options.SetupSwaggerGenOptions(JaneConfiguration.Instance.Root); });
        }
    }
}