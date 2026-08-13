using Microsoft.AspNetCore.Builder;

namespace SmartMonitoring.Shared.Middleware
{
    public static class ExceptionHandlingExtensions
    {
        public static WebApplication UseExceptionHandling(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}
