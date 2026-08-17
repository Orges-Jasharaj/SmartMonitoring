using AuditLogging.Data;
using AuditLogging.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SmartMonitoring.Shared.Observability;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddObservability();

    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen();
    builder.Services.AddMediatR(typeof(Program).Assembly);
    builder.Services.AddMediatRObservability();

    builder.Services.Configure<AuditApiKeyOptions>(builder.Configuration.GetSection(AuditApiKeyOptions.SectionName));

    builder.Services.AddDbContext<AuditDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AuditDbContext>();

    var app = builder.Build();

    app.UseObservability();
    app.UseAuditApiKey();

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

    using (var scope = app.Services.CreateScope())
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (config.GetValue<bool>("Database:RunMigrationsOnStartup"))
        {
            var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            db.Database.Migrate();
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AuditLogging terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
