using Finbuckle.MultiTenant.AspNetCore.Internal;
using Juice.MultiTenant.TestHelper.Internal;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TenantTestServiceProviderExtensions
    {
        public static Task TenantInvokeAsync(this IServiceProvider serviceProvider, RequestDelegate next)
        {
            HttpContext httpContext = new MyHttpContext(serviceProvider);
            return new MultiTenantMiddleware(next).Invoke(httpContext);
        }
    }
}
