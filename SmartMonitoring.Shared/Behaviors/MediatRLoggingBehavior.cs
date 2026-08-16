using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SmartMonitoring.Shared.Behaviors;

public class MediatRLoggingBehavior<TRequest, TResponse>(ILogger<MediatRLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogDebug("Handling MediatR request {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();
            logger.LogInformation(
                "Handled MediatR request {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "MediatR request {RequestName} failed after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
