using AuditLogging.Data;
using AuditLogging.Middleware;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartMonitoring.Shared.Observability;
using System.Text;

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
        });

    builder.Services.AddAuthorization();

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

    app.UseAuthentication();
    app.UseAuthorization();

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
