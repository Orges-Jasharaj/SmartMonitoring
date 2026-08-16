using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;
using SmartMonitoring.Shared.Middleware;

namespace SmartMonitoring.Shared.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        if (string.IsNullOrWhiteSpace(options.ServiceName)
            || options.ServiceName == "UnknownService")
        {
            options.ServiceName = builder.Environment.ApplicationName;
        }

        builder.Services.Configure<ObservabilityOptions>(
            builder.Configuration.GetSection(ObservabilityOptions.SectionName));

        ConfigureSerilog(builder, options);
        ConfigureOpenTelemetry(builder, options);

        return builder;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<MetricsAuthorizationMiddleware>();

        var options = app.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        if (options.EnableRequestLogging)
        {
            app.UseSerilogRequestLogging(opts =>
            {
                opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    var correlationId = httpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                        ?? httpContext.TraceIdentifier;

                    diagnosticContext.Set("CorrelationId", correlationId);
                    diagnosticContext.Set("User", httpContext.User?.Identity?.Name ?? "anonymous");
                };
            });
        }

        if (options.EnablePrometheus && options.EnableOpenTelemetry)
        {
            app.MapPrometheusScrapingEndpoint();
        }

        return app;
    }

    private static void ConfigureSerilog(WebApplicationBuilder builder, ObservabilityOptions options)
    {
        builder.Host.UseSerilog((context, _, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithProperty("ServiceName", options.ServiceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter());

            if (options.EnableLoki)
            {
                configuration.WriteTo.GrafanaLoki(
                    options.LokiUrl,
                    labels:
                    [
                        new LokiLabel { Key = "service", Value = options.ServiceName },
                        new LokiLabel { Key = "environment", Value = context.HostingEnvironment.EnvironmentName }
                    ],
                    propertiesAsLabels: ["level"]);
            }
        });
    }

    private static void ConfigureOpenTelemetry(WebApplicationBuilder builder, ObservabilityOptions options)
    {
        if (!options.EnableOpenTelemetry)
        {
            return;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: options.ServiceName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                        instrumentation.EnrichWithHttpRequest = (activity, request) =>
                        {
                            if (request.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId))
                            {
                                activity.SetTag("correlation.id", correlationId?.ToString());
                            }
                        };
                        instrumentation.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/metrics")
                            && !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource(options.ServiceName);

                if (Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var otlpUri))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = otlpUri);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (options.EnablePrometheus)
                {
                    metrics.AddPrometheusExporter();
                }
            });
    }
}
