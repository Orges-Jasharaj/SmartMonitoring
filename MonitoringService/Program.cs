using MonitoringService.Data;
using MonitoringService.Hubs;
using MonitoringService.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Notifications;
using SmartMonitoring.Shared.Observability;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddObservability();
    builder.Services.AddAuditPublishing(builder.Configuration);
    builder.Services.AddNotificationPublishing(builder.Configuration);
    builder.Services.Configure<AlertOptions>(builder.Configuration.GetSection(AlertOptions.SectionName));

    builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection(IdentityOptions.SectionName));
    builder.Services.AddHttpClient<IIdentityUserEmailResolver, IdentityUserEmailResolver>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
    builder.Services.AddScoped<ICompanyAccessService, CompanyAccessService>();
    builder.Services.AddScoped<IAlertEvaluator, AlertEvaluator>();
    builder.Services.AddScoped<IDeviceOfflineEvaluator, DeviceOfflineEvaluator>();
    builder.Services.AddScoped<IAlertNotificationDispatcher, AlertNotificationDispatcher>();
    builder.Services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();
    builder.Services.AddHostedService<DeviceOfflineMonitorService>();

    builder.Services.AddSignalR();
    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
    builder.Services.AddMediatR(typeof(Program).Assembly);
    builder.Services.AddMediatRObservability();

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection.GetValue<string>("Key")
        ?? throw new InvalidOperationException("Jwt:Key must be configured.");
    var jwtIssuer = jwtSection.GetValue<string>("Issuer")
        ?? throw new InvalidOperationException("Jwt:Issuer must be configured.");
    var jwtAudience = jwtSection.GetValue<string>("Audience")
        ?? throw new InvalidOperationException("Jwt:Audience must be configured.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddDbContext<MonitoringDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<MonitoringDbContext>();

    var app = builder.Build();

    app.UseObservability();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!builder.Configuration.GetValue<bool>("DisableHttpsRedirection"))
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapControllers();
    app.MapHub<MonitoringHub>("/hubs/monitoring");

    using (var scope = app.Services.CreateScope())
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (config.GetValue<bool>("Database:RunMigrationsOnStartup"))
        {
            var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
            db.Database.Migrate();
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MonitoringService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
