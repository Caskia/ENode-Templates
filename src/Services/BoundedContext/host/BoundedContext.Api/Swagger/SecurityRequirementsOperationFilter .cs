using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BoundedContext.Api.Swagger
{
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly IOptions<AuthorizationOptions> authorizationOptions;

        public SecurityRequirementsOperationFilter(IOptions<AuthorizationOptions> authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            var controllerAuthorize = context.ApiDescription.ControllerAttributes().OfType<AuthorizeAttribute>().Cast<Attribute>();
            //var janeControllerAuthorize = context.ApiDescription.ControllerAttributes().OfType<JaneAuthorizeAttribute>().Cast<Attribute>();
            var actionAuthorize = context.ApiDescription.ActionAttributes().OfType<AuthorizeAttribute>().Cast<Attribute>();
            //var janeActionAuthorize = context.ApiDescription.ActionAttributes().OfType<JaneAuthorizeAttribute>().Cast<Attribute>();
            var authorizes = controllerAuthorize/*.Union(janeControllerAuthorize)*/.Union(actionAuthorize)/*.Union(janeActionAuthorize)*/.Distinct();
            if (authorizes.Any())
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>();
                operation.Security.Add(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "Bearer", new List<string>() }
                    });
            }
        }
    }
}