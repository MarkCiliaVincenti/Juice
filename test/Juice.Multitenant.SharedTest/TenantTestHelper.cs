using Finbuckle.MultiTenant.AspNetCore.Internal;
using Microsoft.AspNetCore.Http;

namespace Juice.MultiTenant.SharedTest
{
    public static class TenantTestHelper
    {
        public static Task InnerTenantAsync(IServiceProvider serviceProvider, RequestDelegate next)
        {
            HttpContext httpContext = new MyHttpContext(serviceProvider);
            return new MultiTenantMiddleware(next).Invoke(httpContext);
        }
    }
}
