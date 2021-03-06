﻿using Jane.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.IO;
using System.Reflection;

namespace BoundedContext.Api.Swagger
{
    public static class SwaggerOptionExtensions
    {
        public static void SetupSwaggerGenOptions(this SwaggerGenOptions options, IConfigurationRoot appConfiguration)
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectName BoundedContext Api Explorer", Version = "1" });
            options.SwaggerDoc("admin-v1", new OpenApiInfo { Title = "ProjectName BoundedContext Admin Api Explorer", Version = "1" });

            options.DocInclusionPredicate((docName, description) =>
            {
                if (description.ActionDescriptor.RouteValues.ContainsKey("area"))
                {
                    var area = description.ActionDescriptor.RouteValues["area"];
                    var version = description.ActionDescriptor.AttributeRouteInfo.Template.Split("/")[0];
                    if (!area.IsNullOrEmpty())
                    {
                        return $"{area}-v{version}" == docName;
                    }
                    else
                    {
                        return $"v{version}" == docName;
                    }
                }
                return true;
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Flows = new OpenApiOAuthFlows
                {
                    Password = new OpenApiOAuthFlow()
                    {
                        TokenUrl = new Uri(appConfiguration["IdentityServer:TokenUrl"]),
                    }
                }
            });
            options.IncludeXmlComments(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BoundedContext.Api.xml"));
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.DocumentFilter<ApplyDocumentVendorFilter>();
        }

        public static void SetupSwaggerOptions(this SwaggerOptions options)
        {
            options.RouteTemplate = "docs/{documentName}/json";
        }

        public static void SetupSwaggerUIOptions(this SwaggerUIOptions options)
        {
            options.DocumentTitle = "ProjectName BoundedContext Api V1 Docs";
            options.RoutePrefix = "docs";
            options.SwaggerEndpoint("/docs/v1/json", "ProjectName BoundedContext Api V1 Docs");
            options.SwaggerEndpoint("/docs/admin-v1/json", "ProjectName BoundedContext Admin Api V1 Docs");
            options.IndexStream = () => Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("BoundedContext.Api.Swagger.CustomUI.index.html");

            options.OAuthClientId("resourceowner");
            options.OAuthClientSecret("TPOAuthClientSecret");
            options.OAuthRealm("realm");
            options.OAuthAppName("BoundedContext");
        }
    }
}