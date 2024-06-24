using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Microsoft.Identity.Web.Resource;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Juice.Extensions.Swagger
{
    /// <summary>
    /// Add 401 and 403 response to swagger doc if api has authorize attribute
    /// </summary>
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize =
              (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)?.OfType<AuthorizeAttribute>()?.Any() ?? false)
              || context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            if (hasAuthorize)
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

                var attr = context.MethodInfo.GetCustomAttributes(true).OfType<RequiredScopeAttribute>().FirstOrDefault();
                var controllerAttr = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<RequiredScopeAttribute>().FirstOrDefault();
                var scopes = attr?.AcceptedScope ?? controllerAttr?.AcceptedScope ?? new string[0];

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecurityScheme {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "oauth2"
                                }
                            }
                        ] = scopes
                    }
                };
            }
        }
    }

}
