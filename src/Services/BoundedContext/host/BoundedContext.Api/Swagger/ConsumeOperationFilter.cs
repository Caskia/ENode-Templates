using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace BoundedContext.Api.Swagger
{
    public class ConsumeOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var consumes = operation.Consumes;
            consumes.Clear();
            var consumesAttributes = context.ApiDescription.ActionAttributes().OfType<ConsumesAttribute>();
            foreach (var contentType in consumesAttributes.SelectMany(a => a.ContentTypes))
            {
                consumes.Add(contentType);
            }
        }
    }
}