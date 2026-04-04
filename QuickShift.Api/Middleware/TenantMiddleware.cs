namespace QuickShift.Middleware
{
    public class TenantMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdStr))
            {
                if (int.TryParse(tenantIdStr, out var tenantId))
                {
                    context.Items["TenantId"] = tenantId;
                }
            }

            await _next(context);
        }
    }
}
