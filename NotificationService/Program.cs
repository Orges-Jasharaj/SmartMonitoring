using NotificationService.Middleware;
using NotificationService.Services.Email;
using MediatR;
using Serilog;
using SmartMonitoring.Shared.Observability;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddObservability();

    builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
    builder.Services.Configure<NotificationApiKeyOptions>(builder.Configuration.GetSection(NotificationApiKeyOptions.SectionName));
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen();
    builder.Services.AddMediatR(typeof(Program).Assembly);
    builder.Services.AddMediatRObservability();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseObservability();
    app.UseNotificationApiKey();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!builder.Configuration.GetValue<bool>("DisableHttpsRedirection"))
    {
        app.UseHttpsRedirection();
    }

    app.MapHealthChecks("/health");
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
