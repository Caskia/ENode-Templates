using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BoundedContext.Api.Swagger
{
    public class ApplyDocumentVendorFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            var needToRemove = new List<KeyValuePair<string, PathItem>>();

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