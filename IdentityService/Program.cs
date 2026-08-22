using IdentityService.Data;
using IdentityService.Data.Models;
using IdentityService.Services;
using IdentityService.Services.Email;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartMonitoring.Shared.Middleware;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Observability;
using System;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.Services.AddAuditPublishing(builder.Configuration);

// Add services to the container.

builder.Services.AddControllers();

// Swagger / OpenAPI (Swashbuckle)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\n\nExample: 'Bearer 12345abcdef'",
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
            new string[] { }
        }
    });
});

// MediatR
builder.Services.AddMediatR(typeof(Program).Assembly);
builder.Services.AddMediatRObservability();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityAppDbContext>();

// JWT configuration
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key")
    ?? throw new InvalidOperationException("Jwt:Key must be configured.");
var jwtIssuer = jwtSection.GetValue<string>("Issuer")
    ?? throw new InvalidOperationException("Jwt:Issuer must be configured.");
var jwtAudience = jwtSection.GetValue<string>("Audience")
    ?? throw new InvalidOperationException("Jwt:Audience must be configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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

// JwtService
builder.Services.AddScoped<IJwtService, JwtService>();

// Use shared response envelope for model validation errors
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorMessage = e.ErrorMessage }))
            .ToList();

        var resp = SmartMonitoring.Shared.Dtos.Responses.ResponseDto<object>.Failure("Validation failed", errors);
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(resp);
    };
});

builder.Services.AddDbContext<IdentityAppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var emailEnabled = builder.Configuration.GetValue<bool>("Email:Enabled");
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = emailEnabled;
})
.AddEntityFrameworkStores<IdentityAppDbContext>()
.AddDefaultTokenProviders();

// Ensure JWT is used as the default authentication/challenge scheme (AddIdentity may override defaults)
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

// Prevent cookie auth from redirecting to login page for API requests
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Swagger/OpenAPI configured via AddOpenApi/MapOpenApi

var app = builder.Build();

app.UseObservability();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API V1");
    });
}

if (!builder.Configuration.GetValue<bool>("DisableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

// Authentication (validate JWT)
app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var config = services.GetRequiredService<IConfiguration>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    if (config.GetValue<bool>("Database:RunMigrationsOnStartup"))
    {
        var db = services.GetRequiredService<IdentityAppDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied.");
    }

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<User>>();

    var adminSection = config.GetSection("AdminUser");
    var adminUserName = adminSection.GetValue<string>("UserName") ?? "admin";
    var adminEmail = adminSection.GetValue<string>("Email") ?? "admin@localhost";
    // Prefer environment variables or user-secrets. Example env var name: AdminUser__Password
    // First check explicit environment variable, then configuration (which includes user-secrets when enabled).
    var adminPassword = Environment.GetEnvironmentVariable("AdminUser__Password")
                        ?? config.GetValue<string>("AdminUser:Password");

    // create Admin role if missing
    if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
    {
        var r = roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
        if (!r.Succeeded)
        {
            logger.LogWarning("Could not create Admin role during startup: {Errors}", string.Join(';', r.Errors.Select(e => e.Description)));
        }
    }

    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        logger.LogWarning("Admin password not provided in configuration. Skipping admin user creation. Set AdminUser:Password via user-secrets or environment variable to seed an admin.");
    }
    else
    {
        // create admin user if not exists
        var adminUser = userManager.FindByNameAsync(adminUserName).GetAwaiter().GetResult();
        if (adminUser == null)
        {
            var user = new User
            {
                UserName = adminUserName,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                CreatedAt = DateTime.UtcNow,
                isActive = true,
                EmailConfirmed = true
            };
            var result = userManager.CreateAsync(user, adminPassword).GetAwaiter().GetResult();
            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(user, "Admin").GetAwaiter().GetResult();
                logger.LogInformation("Admin user created: {UserName}", adminUserName);
            }
            else
            {
                logger.LogWarning("Failed to create admin user: {Errors}", string.Join(';', result.Errors.Select(e => e.Description)));
            }
        }
    }
}

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "IdentityService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
