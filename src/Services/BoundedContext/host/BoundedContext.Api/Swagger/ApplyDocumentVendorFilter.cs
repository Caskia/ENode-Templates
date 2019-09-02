using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BoundedContext.Api.Swagger
{
    public class ApplyDocumentVendorFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var needToRemove = new List<KeyValuePair<string, OpenApiPathItem>>();

            //upper paths
            Regex parameterRegex = new Regex(@"{\w+}");
            foreach (var path in swaggerDoc.Paths)
            {
                var removeParameterPath = parameterRegex.Replace(path.Key, "");
                if (removeParameterPath != removeParameterPath.ToLower())
                {
                    needToRemove.Add(path);
                }
            }

            foreach (var item in needToRemove)
            {
                swaggerDoc.Paths.Remove(item.Key);
            }
        }
    }
}