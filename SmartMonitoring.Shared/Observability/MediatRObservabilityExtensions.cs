using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartMonitoring.Shared.Behaviors;

namespace SmartMonitoring.Shared.Observability;

public static class MediatRObservabilityExtensions
{
    public static IServiceCollection AddMediatRObservability(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatRLoggingBehavior<,>));
        return services;
    }
}
